//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     MongoDbResourceBuilderExtensions.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  AppHost
//=======================================================

using System.Globalization;
using System.Text;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

using MongoDB.Bson;
using MongoDB.Driver;

namespace Aspire.Hosting;

internal static partial class MongoDbResourceBuilderExtensions
{
	// Shared semaphore — guards all three dev commands (Clear, Seed, Stats) so only one runs at a time.
	private static readonly SemaphoreSlim _dbMutex = new(1, 1);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Clear MyBlog data skipped on {ResourceName} — a clear operation is already in progress.")]
	private static partial void LogClearSkipped(ILogger logger, string resourceName);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Clear MyBlog data invoked on {ResourceName} — enumerating collections in '{Database}'.")]
	private static partial void LogClearStarted(ILogger logger, string resourceName, string database);

	[LoggerMessage(Level = LogLevel.Error, Message = "Could not resolve MongoDB connection string for resource {ResourceName}.")]
	private static partial void LogConnectionStringError(ILogger logger, string resourceName);

	[LoggerMessage(Level = LogLevel.Information, Message = "Collection '{Collection}': {Count} document(s) deleted.")]
	private static partial void LogCollectionDeleted(ILogger logger, string collection, long count);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Collection '{Collection}' could not be cleared — skipping and continuing.")]
	private static partial void LogCollectionClearError(ILogger logger, Exception exception, string collection);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Clear MyBlog data complete: {Total} document(s) removed across {Count} collection(s). Warnings: {WarnCount}.")]
	private static partial void LogClearComplete(ILogger logger, long total, int count, int warnCount);

	[LoggerMessage(Level = LogLevel.Warning, Message = "Seed MyBlog data skipped on {ResourceName} — a database operation is already in progress.")]
	private static partial void LogSeedSkipped(ILogger logger, string resourceName);

	[LoggerMessage(Level = LogLevel.Information, Message = "Seed MyBlog data invoked on {ResourceName} — upserting 7 canonical categories and inserting blog posts into '{Database}'.")]
	private static partial void LogSeedStarted(ILogger logger, string resourceName, string database);

	[LoggerMessage(Level = LogLevel.Information, Message = "Seed MyBlog data complete: 7 categories upserted + {Count} blog post(s) inserted.")]
	private static partial void LogSeedComplete(ILogger logger, int count);

	private static readonly string[] LegacyCollectionsToDrop = ["posts", "tags"];

	[LoggerMessage(Level = LogLevel.Warning, Message = "Show MyBlog stats skipped on {ResourceName} — a database operation is already in progress.")]
	private static partial void LogStatsSkipped(ILogger logger, string resourceName);

	[LoggerMessage(Level = LogLevel.Information, Message = "Show MyBlog stats invoked on {ResourceName} — querying '{Database}'.")]
	private static partial void LogStatsStarted(ILogger logger, string resourceName, string database);

	[LoggerMessage(Level = LogLevel.Information, Message = "Show MyBlog stats complete: {Count} collection(s) reported.")]
	private static partial void LogStatsComplete(ILogger logger, int count);

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
			if (!await _dbMutex.WaitAsync(0).ConfigureAwait(false))
			{
				LogClearSkipped(context.Logger, context.ResourceName);

				return new ExecuteCommandResult
				{
					Success = false,
					Message = "A clear operation is already in progress. Wait for the current run to finish, then try again."
				};
			}

			try
			{
				LogClearStarted(context.Logger, context.ResourceName, databaseName);

				var connectionString = await builder.Resource.ConnectionStringExpression.GetValueAsync(context.CancellationToken).ConfigureAwait(false);
				if (connectionString is null)
				{
					LogConnectionStringError(context.Logger, context.ResourceName);
					return new ExecuteCommandResult
					{
						Success = false,
						Message = "Could not resolve MongoDB connection string. Is the MongoDB resource running?"
					};
				}

				var client = new MongoClient(connectionString);
				var database = client.GetDatabase(databaseName);

				var namesCursor = await database.ListCollectionNamesAsync(cancellationToken: context.CancellationToken).ConfigureAwait(false);
				var collectionNames = await namesCursor.ToListAsync(context.CancellationToken).ConfigureAwait(false);

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
				context.CancellationToken).ConfigureAwait(false);

						results.Add((name, deleteResult.DeletedCount));

						LogCollectionDeleted(context.Logger, name, deleteResult.DeletedCount);
					}
					catch (Exception ex) when (ex is not OperationCanceledException)
					{
						var warning = $"{name}: {ex.Message}";
						warnings.Add(warning);
						LogCollectionClearError(context.Logger, ex, name);
					}
				}

				var totalDeleted = results.Sum(static r => r.Deleted);
				var perCollection = results.Count == 0
		? "no non-system collections found"
		: string.Join("; ", results.Select(static r => $"{r.Name}: {r.Deleted}"));

				LogClearComplete(context.Logger, totalDeleted, results.Count, warnings.Count);

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
			if (!await _dbMutex.WaitAsync(0).ConfigureAwait(false))
			{
				LogSeedSkipped(context.Logger, context.ResourceName);

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
					await afterMutexAcquired(context.CancellationToken).ConfigureAwait(false);

				LogSeedStarted(context.Logger, context.ResourceName, databaseName);

				var connectionString = await builder.Resource.ConnectionStringExpression.GetValueAsync(context.CancellationToken).ConfigureAwait(false);
				if (connectionString is null)
				{
					LogConnectionStringError(context.Logger, context.ResourceName);
					return new ExecuteCommandResult
					{
						Success = false,
						Message = "Could not resolve MongoDB connection string. Is the MongoDB resource running?"
					};
				}

				var client = new MongoClient(connectionString);
				var database = client.GetDatabase(databaseName);
				var collectionNames = await (await database.ListCollectionNamesAsync(cancellationToken: context.CancellationToken).ConfigureAwait(false))
					.ToListAsync(context.CancellationToken)
					.ConfigureAwait(false);

				var droppedLegacyCollections = new List<string>();
				foreach (var legacyCollectionName in LegacyCollectionsToDrop)
				{
					if (!collectionNames.Contains(legacyCollectionName, StringComparer.Ordinal))
					{
						continue;
					}

					await database.DropCollectionAsync(legacyCollectionName, context.CancellationToken).ConfigureAwait(false);
					droppedLegacyCollections.Add(legacyCollectionName);
				}

				var categoriesCollection = database.GetCollection<BsonDocument>("categories");
				var postsCollection = database.GetCollection<BsonDocument>("blogposts");

				var now = DateTime.UtcNow;

				// Canonical categories — stable ObjectIds from docs/Category-Seed-Data.
				// Never change these IDs; blog posts and tests rely on them as foreign keys.
				var canonicalCategories = new BsonDocument[]
				{
					new() { ["_id"] = new ObjectId("677db927900ea4af1b500cab"), ["Name"] = "ASP.NET Core",                      ["Description"] = "This document is related to ASP.NET Core" },
					new() { ["_id"] = new ObjectId("677db927900ea4af1b500cac"), ["Name"] = "Blazor Server",                     ["Description"] = "This document is related to Blazor Server" },
					new() { ["_id"] = new ObjectId("677db9bd900ea4af1b500cad"), ["Name"] = "Blazor WebAssembly",                ["Description"] = "This document is related to Blazor WebAssembly" },
					new() { ["_id"] = new ObjectId("677db9bd900ea4af1b500cae"), ["Name"] = "C#",                               ["Description"] = "This document is related to C#" },
					new() { ["_id"] = new ObjectId("677db9bd900ea4af1b500caf"), ["Name"] = "Entity Framework Core (EF Core)",  ["Description"] = "This document is related to Entity Framework Core (EF Core)" },
					new() { ["_id"] = new ObjectId("677db9bd900ea4af1b500cb0"), ["Name"] = ".NET MAUI",                       ["Description"] = "This document is related to .NET MAUI" },
					new() { ["_id"] = new ObjectId("677db9bd900ea4af1b500cb1"), ["Name"] = "Other",                           ["Description"] = "This document is related to other information" },
				};

				foreach (var category in canonicalCategories)
				{
					await categoriesCollection.ReplaceOneAsync(
						Builders<BsonDocument>.Filter.Eq("_id", category["_id"]),
						category,
						new ReplaceOptions { IsUpsert = true },
						cancellationToken: context.CancellationToken).ConfigureAwait(false);
				}

				// Canonical category IDs referenced by seed blog posts.
				var aspNetCoreId = new ObjectId("677db927900ea4af1b500cab");
				var otherId = new ObjectId("677db9bd900ea4af1b500cb1");

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
["_id"] = new ObjectId("000000000000000000000002"),
["Title"] = "Welcome to MyBlog",
["Content"] = "This is the first post on MyBlog. Welcome!",
["Author"] = authorDocument.DeepClone(),
["CreatedAt"] = now,
["UpdatedAt"] = now,
["IsPublished"] = true,
["Version"] = 1,
["CategoryId"] = otherId,
},
new()
{
["_id"] = new ObjectId("000000000000000000000003"),
["Title"] = "Getting Started with .NET Aspire",
["Content"] = "Learn how to build cloud-native apps with .NET Aspire.",
["Author"] = authorDocument.DeepClone(),
["CreatedAt"] = now,
["UpdatedAt"] = now,
["IsPublished"] = true,
["Version"] = 1,
["CategoryId"] = aspNetCoreId,
},
new()
{
["_id"] = new ObjectId("000000000000000000000004"),
["Title"] = "Draft: MongoDB Performance Tips",
["Content"] = "Work in progress — tips for optimising MongoDB queries.",
["Author"] = authorDocument.DeepClone(),
["CreatedAt"] = now,
["UpdatedAt"] = now,
["IsPublished"] = false,
["Version"] = 1,
["CategoryId"] = otherId,
},
	};

				await postsCollection.InsertManyAsync(seedDocuments, cancellationToken: context.CancellationToken).ConfigureAwait(false);

				LogSeedComplete(context.Logger, seedDocuments.Length);

				return new ExecuteCommandResult
				{
					Success = true,
					Message = $"categories: 7 upserted (ASP.NET Core, Blazor Server, Blazor WebAssembly, C#, EF Core, .NET MAUI, Other); blogposts: {seedDocuments.Length} inserted (2 published, 1 draft)"
						+ (droppedLegacyCollections.Count == 0
							? string.Empty
							: $"; dropped legacy collections: {string.Join(", ", droppedLegacyCollections)}")
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
			if (!await _dbMutex.WaitAsync(0).ConfigureAwait(false))
			{
				LogStatsSkipped(context.Logger, context.ResourceName);

				return CommandResults.Failure(
		"A database operation is already in progress. Wait for the current run to finish, then try again.");
			}

			try
			{
				LogStatsStarted(context.Logger, context.ResourceName, databaseName);

				var connectionString = await builder.Resource.ConnectionStringExpression.GetValueAsync(context.CancellationToken).ConfigureAwait(false);
				if (connectionString is null)
				{
					LogConnectionStringError(context.Logger, context.ResourceName);
					return CommandResults.Failure("Could not resolve MongoDB connection string. Is the MongoDB resource running?");
				}

				var client = new MongoClient(connectionString);
				var database = client.GetDatabase(databaseName);

				var namesCursor = await database.ListCollectionNamesAsync(cancellationToken: context.CancellationToken).ConfigureAwait(false);
				var collectionNames = await namesCursor.ToListAsync(context.CancellationToken).ConfigureAwait(false);
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
				cancellationToken: context.CancellationToken).ConfigureAwait(false);
						sb.AppendLine(CultureInfo.InvariantCulture, $"| {name} | {count} |");
					}
				}

				var markdownTable = sb.ToString();
				LogStatsComplete(context.Logger, userCollections.Count);

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
