//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     MongoDbResourceBuilderExtensions.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  AppHost
//=======================================================

using System.Text;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

using MongoDB.Bson;
using MongoDB.Driver;

namespace Aspire.Hosting;

internal static class MongoDbResourceBuilderExtensions
{
	// Shared semaphore — guards all three dev commands (Clear, Seed, Stats) so only one runs at a time.
	private static readonly SemaphoreSlim _dbMutex = new(1, 1);

	/// <summary>
	/// Test-only hook used by <c>AppHost.Tests</c> to hold the seed command inside the shared mutex
	/// so overlapping invocations can be asserted deterministically.
	/// </summary>
	internal static Func<CancellationToken, ValueTask>? SeedCommandAfterMutexAcquiredAsync { get; set; }

	public static IResourceBuilder<MongoDBServerResource> WithMongoDbDevCommands(
	this IResourceBuilder<MongoDBServerResource> builder,
	string databaseName)
	{
		if (!builder.ApplicationBuilder.ExecutionContext.IsRunMode)
			return builder;

		builder.WithClearDatabaseCommand(databaseName);
		builder.WithSeedDataCommand(databaseName);
		builder.WithShowStatsCommand(databaseName);
		return builder;
	}

	private static void WithClearDatabaseCommand(
	this IResourceBuilder<MongoDBServerResource> builder,
	string databaseName)
	{
		builder.WithCommand(
		"clear-myblog-data",
		"⚠️ Clear MyBlog Data",
		executeCommand: async context =>
		{
			// AC2: Non-blocking acquire — return immediately if another clear is already in flight.
			if (!await _dbMutex.WaitAsync(0))
			{
				context.Logger.LogWarning(
		"Clear MyBlog data skipped on {ResourceName} — a clear operation is already in progress.",
		context.ResourceName);

				return new ExecuteCommandResult
				{
					Success = false,
					Message = "A clear operation is already in progress. Wait for the current run to finish, then try again."
				};
			}

			try
			{
				context.Logger.LogWarning(
		"Clear MyBlog data invoked on {ResourceName} — enumerating collections in '{Database}'.",
		context.ResourceName, databaseName);

				var connectionString = await builder.Resource.ConnectionStringExpression.GetValueAsync(context.CancellationToken);
				if (connectionString is null)
				{
					context.Logger.LogError("Could not resolve MongoDB connection string for resource {ResourceName}.", context.ResourceName);
					return new ExecuteCommandResult
					{
						Success = false,
						Message = "Could not resolve MongoDB connection string. Is the MongoDB resource running?"
					};
				}

				var client = new MongoClient(connectionString);
				var database = client.GetDatabase(databaseName);

				var namesCursor = await database.ListCollectionNamesAsync(cancellationToken: context.CancellationToken);
				var collectionNames = await namesCursor.ToListAsync(context.CancellationToken);

				var results = new List<(string Name, long Deleted)>();
				var warnings = new List<string>();

				foreach (var name in collectionNames)
				{
					// Skip MongoDB internal system collections (e.g. system.views, system.users).
					if (name.StartsWith("system.", StringComparison.OrdinalIgnoreCase))
						continue;

					try
					{
						// AC3 (#249): Best-effort per collection — errors are caught, logged as warnings,
						// and the loop continues so remaining collections are still processed.
						var collection = database.GetCollection<BsonDocument>(name);
						var deleteResult = await collection.DeleteManyAsync(
				FilterDefinition<BsonDocument>.Empty,
				context.CancellationToken);

						results.Add((name, deleteResult.DeletedCount));

						context.Logger.LogInformation(
				"Collection '{Collection}': {Count} document(s) deleted.",
				name, deleteResult.DeletedCount);
					}
					catch (Exception ex) when (ex is not OperationCanceledException)
					{
						var warning = $"{name}: {ex.Message}";
						warnings.Add(warning);
						context.Logger.LogWarning(
				ex,
				"Collection '{Collection}' could not be cleared — skipping and continuing.",
				name);
					}
				}

				var totalDeleted = results.Sum(static r => r.Deleted);
				var perCollection = results.Count == 0
		? "no non-system collections found"
		: string.Join("; ", results.Select(static r => $"{r.Name}: {r.Deleted}"));

				context.Logger.LogWarning(
		"Clear MyBlog data complete: {Total} document(s) removed across {Count} collection(s). Warnings: {WarnCount}.",
		totalDeleted, results.Count, warnings.Count);

				var message = $"{results.Count} collection(s) cleared — {totalDeleted} total document(s) deleted. ({perCollection})";
				if (warnings.Count > 0)
					message += $" ⚠️ {warnings.Count} collection(s) had errors: {string.Join("; ", warnings)}";

				return new ExecuteCommandResult
				{
					Success = true,
					Message = message
				};
			}
			finally
			{
				_dbMutex.Release();
			}
		},
		new CommandOptions
		{
			Description = "Permanently deletes all data from the myblog database. Local development only.",
			ConfirmationMessage = "This will permanently delete ALL data from the myblog database and cannot be undone. Confirm?",
			IsHighlighted = true,
			IconName = "DatabaseWarning",
			// AC1 (#249): Gates only on the MongoDB resource's own health — intentionally does NOT
			// check dependent resources (Web, etc.). Clearing is valid while the app is live against
			// local Mongo; the Web app running is not a reason to disable the command.
			UpdateState = ctx =>
	ctx.ResourceSnapshot.HealthStatus == HealthStatus.Healthy
	? ResourceCommandState.Enabled
	: ResourceCommandState.Disabled
		});
	}

	private static void WithSeedDataCommand(
	this IResourceBuilder<MongoDBServerResource> builder,
	string databaseName)
	{
		builder.WithCommand(
		"seed-myblog-data",
		"🌱 Seed MyBlog Data",
		executeCommand: async context =>
		{
			if (!await _dbMutex.WaitAsync(0))
			{
				context.Logger.LogWarning(
		"Seed MyBlog data skipped on {ResourceName} — a database operation is already in progress.",
		context.ResourceName);

				return new ExecuteCommandResult
				{
					Success = false,
					Message = "A database operation is already in progress. Wait for the current run to finish, then try again."
				};
			}

			try
			{
				var afterMutexAcquired = SeedCommandAfterMutexAcquiredAsync;
				if (afterMutexAcquired is not null)
					await afterMutexAcquired(context.CancellationToken);

				context.Logger.LogInformation(
		"Seed MyBlog data invoked on {ResourceName} — upserting General category and inserting blog posts into '{Database}'.",
		context.ResourceName, databaseName);

				var connectionString = await builder.Resource.ConnectionStringExpression.GetValueAsync(context.CancellationToken);
				if (connectionString is null)
				{
					context.Logger.LogError("Could not resolve MongoDB connection string for resource {ResourceName}.", context.ResourceName);
					return new ExecuteCommandResult
					{
						Success = false,
						Message = "Could not resolve MongoDB connection string. Is the MongoDB resource running?"
					};
				}

				var client = new MongoClient(connectionString);
				var database = client.GetDatabase(databaseName);

				var categoriesCollection = database.GetCollection<BsonDocument>("categories");
				var postsCollection = database.GetCollection<BsonDocument>("blogposts");

				var now = DateTime.UtcNow;

				// Seed the default "General" category with a stable, well-known id.
				var generalCategoryId = new BsonBinaryData(
					new Guid("00000000-0000-0000-0000-000000000001"),
					GuidRepresentation.Standard);

				var generalCategory = new BsonDocument
				{
					["_id"] = generalCategoryId,
					["Name"] = "General",
					["Description"] = "Default category for blog posts.",
				};
				await categoriesCollection.ReplaceOneAsync(
					Builders<BsonDocument>.Filter.Eq("_id", generalCategoryId),
					generalCategory,
					new ReplaceOptions { IsUpsert = true },
					cancellationToken: context.CancellationToken);

				var authorId = "auth0|author-matthew-paulosky";
				var authorDocument = new BsonDocument
				{
					["AuthorId"] = authorId,
					["AuthorName"] = "Matthew Paulosky",
					["AuthorEmail"] = "matthew@paulosky.com",
					["AuthorRoles"] = new BsonArray { "Author", "Admin" }
				};

				var seedDocuments = new BsonDocument[]
		{
new()
{
["_id"] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard),
["Title"] = "Welcome to MyBlog",
["Content"] = "This is the first post on MyBlog. Welcome!",
["Author"] = authorDocument.DeepClone(),
["CreatedAt"] = now,
["UpdatedAt"] = now,
["IsPublished"] = true,
["Version"] = 1,
["CategoryId"] = generalCategoryId,
},
new()
{
["_id"] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard),
["Title"] = "Getting Started with .NET Aspire",
["Content"] = "Learn how to build cloud-native apps with .NET Aspire.",
["Author"] = authorDocument.DeepClone(),
["CreatedAt"] = now,
["UpdatedAt"] = now,
["IsPublished"] = true,
["Version"] = 1,
["CategoryId"] = generalCategoryId,
},
new()
{
["_id"] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard),
["Title"] = "Draft: MongoDB Performance Tips",
["Content"] = "Work in progress — tips for optimising MongoDB queries.",
["Author"] = authorDocument.DeepClone(),
["CreatedAt"] = now,
["UpdatedAt"] = now,
["IsPublished"] = false,
["Version"] = 1,
["CategoryId"] = generalCategoryId,
},
	};

				await postsCollection.InsertManyAsync(seedDocuments, cancellationToken: context.CancellationToken);

				context.Logger.LogInformation(
		"Seed MyBlog data complete: 1 category upserted + {Count} blog post(s) inserted.",
		seedDocuments.Length);

				return new ExecuteCommandResult
				{
					Success = true,
					Message = $"categories: 1 upserted (General); blogposts: {seedDocuments.Length} inserted (2 published, 1 draft)"
				};
			}
			finally
			{
				_dbMutex.Release();
			}
		},
		new CommandOptions
		{
			Description = "Inserts seed blog posts into the myblog database. Local development only.",
			IconName = "DatabaseArrowUp",
			UpdateState = ctx =>
	ctx.ResourceSnapshot.HealthStatus == HealthStatus.Healthy
	? ResourceCommandState.Enabled
	: ResourceCommandState.Disabled
		});
	}

	private static void WithShowStatsCommand(
	this IResourceBuilder<MongoDBServerResource> builder,
	string databaseName)
	{
		builder.WithCommand(
		"show-myblog-stats",
		"📊 Show MyBlog Stats",
		executeCommand: async context =>
		{
			if (!await _dbMutex.WaitAsync(0))
			{
				context.Logger.LogWarning(
		"Show MyBlog stats skipped on {ResourceName} — a database operation is already in progress.",
		context.ResourceName);

				return CommandResults.Failure(
		"A database operation is already in progress. Wait for the current run to finish, then try again.");
			}

			try
			{
				context.Logger.LogInformation(
		"Show MyBlog stats invoked on {ResourceName} — querying '{Database}'.",
		context.ResourceName, databaseName);

				var connectionString = await builder.Resource.ConnectionStringExpression.GetValueAsync(context.CancellationToken);
				if (connectionString is null)
				{
					context.Logger.LogError("Could not resolve MongoDB connection string for resource {ResourceName}.", context.ResourceName);
					return CommandResults.Failure("Could not resolve MongoDB connection string. Is the MongoDB resource running?");
				}

				var client = new MongoClient(connectionString);
				var database = client.GetDatabase(databaseName);

				var namesCursor = await database.ListCollectionNamesAsync(cancellationToken: context.CancellationToken);
				var collectionNames = await namesCursor.ToListAsync(context.CancellationToken);
				var userCollections = collectionNames
		.Where(static n => !n.StartsWith("system.", StringComparison.OrdinalIgnoreCase))
		.ToList();

				var sb = new StringBuilder();
				sb.AppendLine("| Collection | Document Count |");
				sb.AppendLine("| --- | --- |");

				if (userCollections.Count == 0)
				{
					sb.AppendLine("| *(no collections found)* | - |");
				}
				else
				{
					foreach (var name in userCollections)
					{
						var col = database.GetCollection<BsonDocument>(name);
						var count = await col.CountDocumentsAsync(
				FilterDefinition<BsonDocument>.Empty,
				cancellationToken: context.CancellationToken);
						sb.AppendLine($"| {name} | {count} |");
					}
				}

				var markdownTable = sb.ToString();
				context.Logger.LogInformation(
		"Show MyBlog stats complete: {Count} collection(s) reported.",
		userCollections.Count);

				return CommandResults.Success(
		$"{userCollections.Count} collection(s) found in '{databaseName}'",
		new CommandResultData
		{
			Value = markdownTable,
			Format = CommandResultFormat.Markdown,
			DisplayImmediately = true
		});
			}
			finally
			{
				_dbMutex.Release();
			}
		},
		new CommandOptions
		{
			Description = "Displays document counts per collection in the myblog database. Local development only.",
			IconName = "ChartMultiple",
			UpdateState = ctx =>
	ctx.ResourceSnapshot.HealthStatus == HealthStatus.Healthy
	? ResourceCommandState.Enabled
	: ResourceCommandState.Disabled
		});
	}
}
