//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     AppHost.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  AppHost
//=======================================================
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

using MongoDB.Bson;
using MongoDB.Driver;

var builder = DistributedApplication.CreateBuilder(args);

var mongo = builder.AddMongoDB("mongodb")
	.WithDataVolume("mongo-data");
var mongoDb = mongo.AddDatabase("myblog");
var redis = builder.AddRedis("redis");

// AC2 (#249): Semaphore prevents overlapping clear runs. A second concurrent invocation
// returns immediately with operator feedback instead of racing against the first.
var clearMutex = new SemaphoreSlim(1, 1);

// Expose the destructive clear-data action only during local runs (IsRunMode = false when publishing).
if (builder.ExecutionContext.IsRunMode)
{
	mongo.WithCommand(
		"clear-myblog-data",
		"⚠️ Clear MyBlog Data",
		executeCommand: async context =>
		{
			// AC2: Non-blocking acquire — return immediately if another clear is already in flight.
			if (!await clearMutex.WaitAsync(0))
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
					"Clear MyBlog data invoked on {ResourceName} — enumerating collections in 'myblog'.",
					context.ResourceName);

				var connectionString = await mongo.Resource.ConnectionStringExpression.GetValueAsync(context.CancellationToken);
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
				var database = client.GetDatabase("myblog");

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
				clearMutex.Release();
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

builder.AddProject<Projects.Web>("web")
		.WithReference(mongoDb)
		.WithReference(redis)
		.WaitFor(mongo)
		.WaitFor(redis);

builder.Build().Run();

// Exclude the compiler-generated Program class from coverage.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Aspire host bootstrap — not business logic")]
public partial class Program { }
