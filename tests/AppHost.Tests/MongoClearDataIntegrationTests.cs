// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     MongoClearDataIntegrationTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : MyBlog
// Project Name :  AppHost.Tests
// =============================================

using AppHost.Tests.Infrastructure;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using MongoDB.Bson;
using MongoDB.Driver;

namespace AppHost.Tests;

/// <summary>
/// Full integration tests for the "clear-myblog-data" operator command (issue #247 / #248).
/// <para>
/// These tests require Docker because they boot the real Aspire host so that the handler's
/// closure-captured <c>mongo.Resource.ConnectionStringExpression.GetValueAsync()</c> can
/// resolve the live container's connection string.
/// </para>
/// </summary>
[Collection("MongoClearIntegration")]
public sealed class MongoClearDataIntegrationTests(ClearCommandAppFixture fixture)
{
	private const string CommandName = "clear-myblog-data";

	/// <summary>
	/// After the command runs, all documents are removed from every non-system collection
	/// but the collection shells themselves are preserved (DeleteMany, not DropCollection).
	/// </summary>
	[Fact]
	public async Task ClearMyBlogData_Removes_All_Documents_And_Preserves_Collection_Shells()
	{
		// Arrange
		var seededDatabase = await DropAndSeedAsync(new Dictionary<string, int>
		{
			["posts"] = 5,
			["comments"] = 3,
		});
		using var mongoClient = seededDatabase.Client;
		var db = seededDatabase.Database;

		var annotation = GetAnnotation();
		var ctx = MakeContext();

		// Act
		var result = await annotation.ExecuteCommand(ctx);

		// Assert
		result.Success.Should().BeTrue("the handler must succeed when MongoDB is reachable");

		(await db.GetCollection<BsonDocument>("posts")
			.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: TestContext.Current.CancellationToken))
			.Should().Be(0, "all posts documents must be deleted");

		(await db.GetCollection<BsonDocument>("comments")
			.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: TestContext.Current.CancellationToken))
			.Should().Be(0, "all comments documents must be deleted");

		var names = await (await db.ListCollectionNamesAsync(cancellationToken: TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken);
		names.Should().Contain("posts", "the posts collection shell must be preserved after clearing");
		names.Should().Contain("comments", "the comments collection shell must be preserved after clearing");
	}

	/// <summary>
	/// The result message includes per-collection document-deleted counts so the operator
	/// can see exactly what was removed.
	/// </summary>
	[Fact]
	public async Task ClearMyBlogData_Result_Message_Includes_Per_Collection_Deleted_Counts()
	{
		// Arrange
		var seededDatabase = await DropAndSeedAsync(new Dictionary<string, int>
		{
			["posts"] = 4,
			["tags"] = 2,
		});
		using var mongoClient = seededDatabase.Client;

		var annotation = GetAnnotation();
		var ctx = MakeContext();

		// Act
		var result = await annotation.ExecuteCommand(ctx);

		// Assert
		result.Success.Should().BeTrue();
		result.Message.Should().Contain("posts: 4", "per-collection deleted count must appear in the result message");
		result.Message.Should().Contain("tags: 2", "per-collection deleted count must appear in the result message");
	}

	/// <summary>
	/// Collections that exist but are already empty still appear in the result message with
	/// a count of 0 (they are included in the cleared-collection list).
	/// </summary>
	[Fact]
	public async Task ClearMyBlogData_Empty_Collections_Appear_In_Result_With_Zero_Count()
	{
		// Arrange
		var seededDatabase = await DropAndSeedAsync(new Dictionary<string, int>
		{
			["posts"] = 1,
			["empty-collection"] = 0,
		});
		using var mongoClient = seededDatabase.Client;

		var annotation = GetAnnotation();
		var ctx = MakeContext();

		// Act
		var result = await annotation.ExecuteCommand(ctx);

		// Assert
		result.Success.Should().BeTrue();
		result.Message.Should().Contain("empty-collection: 0",
			"a collection that was already empty must appear in the result with count 0");
	}

	/// <summary>
	/// Two overlapping clear attempts must not run together: exactly one proceeds and the other
	/// fails fast with operator-visible feedback.
	/// </summary>
	[Fact]
	public async Task ClearMyBlogData_Concurrent_Invocations_Allow_Only_One_Run()
	{
		// Arrange
		var seededDatabase = await DropAndSeedAsync(new Dictionary<string, int>
		{
			["posts"] = 50,
			["comments"] = 50,
			["tags"] = 50,
		});
		using var mongoClient = seededDatabase.Client;

		var annotation = GetAnnotation();

		// Act — dispatch both calls to thread-pool workers and open the gate at the same
		// moment so they race to acquire _dbMutex.  Without this the async lambda may run
		// entirely synchronously (fast local MongoDB) and release the semaphore before the
		// second call even starts, causing both to succeed (flake).
		var ct = TestContext.Current.CancellationToken;
		using var startGate = new SemaphoreSlim(0, 2);

		var firstTask = Task.Run(async () =>
		{
			await startGate.WaitAsync(ct);
			return await annotation.ExecuteCommand(MakeContext());
		}, ct);

		var secondTask = Task.Run(async () =>
		{
			await startGate.WaitAsync(ct);
			return await annotation.ExecuteCommand(MakeContext());
		}, ct);

		startGate.Release(2); // open the gate — both workers race for _dbMutex
		var results = await Task.WhenAll(firstTask, secondTask);

		// Assert
		results.Count(static r => r.Success).Should().Be(1,
			"the semaphore should allow only one clear operation to run at a time");
		results.Count(static r => !r.Success).Should().Be(1,
			"the overlapping clear attempt should fail fast instead of queueing");
		results.Single(static r => !r.Success).Message.Should().Contain("already in progress",
			"the operator needs immediate feedback when another clear is in flight");
	}

	/// <summary>
	/// A failure clearing one collection must be reported as a warning while the remaining
	/// collections still clear successfully.
	/// </summary>
	[Fact]
	public async Task ClearMyBlogData_Collection_Failure_Is_Reported_As_Warning_And_Other_Collections_Continue()
	{
		// Arrange
		var seededDatabase = await DropAndSeedAsync(new Dictionary<string, int>
		{
			["posts"] = 2,
			["tags"] = 1,
		});
		using var mongoClient = seededDatabase.Client;
		var db = seededDatabase.Database;
		await CreateViewAsync(db, "posts-readonly-view", "posts");

		var annotation = GetAnnotation();

		// Act
		var result = await annotation.ExecuteCommand(MakeContext());

		// Assert
		result.Success.Should().BeTrue(
			"per-collection failures should be downgraded to warnings so the overall clear can continue");
		result.Message.Should().Contain("posts: 2");
		result.Message.Should().Contain("tags: 1");
		result.Message.Should().Contain("1 collection(s) had errors");
		result.Message.Should().Contain("posts-readonly-view:",
			"the warning summary should identify the collection that could not be cleared");

		(await db.GetCollection<BsonDocument>("posts")
			.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: TestContext.Current.CancellationToken))
			.Should().Be(0, "successful collections should still be cleared when one collection fails");

		(await db.GetCollection<BsonDocument>("tags")
			.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: TestContext.Current.CancellationToken))
			.Should().Be(0, "collections after the failure should still be processed");
	}

	// ---------------------------------------------------------------------------
	// Helpers
	// ---------------------------------------------------------------------------

	private async Task<(MongoClient Client, IMongoDatabase Database)> DropAndSeedAsync(Dictionary<string, int> collections)
	{
		var client = new MongoClient(fixture.MongoConnectionString);
		await client.DropDatabaseAsync("myblog", TestContext.Current.CancellationToken);
		var db = client.GetDatabase("myblog");

		foreach (var (name, count) in collections)
		{
			await db.CreateCollectionAsync(name, cancellationToken: TestContext.Current.CancellationToken);

			if (count > 0)
			{
				var docs = Enumerable
					.Range(0, count)
					.Select(i => new BsonDocument("n", i))
					.ToList();

				await db.GetCollection<BsonDocument>(name)
					.InsertManyAsync(docs, cancellationToken: TestContext.Current.CancellationToken);
			}
		}

		return (client, db);
	}

	private static async Task CreateViewAsync(IMongoDatabase db, string viewName, string sourceCollection)
	{
		var createViewCommand = new BsonDocument
		{
			{ "create", viewName },
			{ "viewOn", sourceCollection },
			{ "pipeline", new BsonArray() },
		};

		await db.RunCommandAsync<BsonDocument>(createViewCommand, cancellationToken: TestContext.Current.CancellationToken);
	}

	private ResourceCommandAnnotation GetAnnotation()
	{
		var mongoResource = fixture.Builder.Resources.Single(static r => r.Name == "mongodb");

		return mongoResource.Annotations
			.OfType<ResourceCommandAnnotation>()
			.Single(static a => a.Name == CommandName);
	}

	private static ExecuteCommandContext MakeContext() => new()
	{
		ResourceName = "mongodb",
		ServiceProvider = new ServiceCollection().BuildServiceProvider(),
		Logger = NullLogger.Instance,
		CancellationToken = TestContext.Current.CancellationToken,
	};
}
