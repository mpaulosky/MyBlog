// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     MongoSeedDataIntegrationTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : MyBlog
// Project Name :  AppHost.Tests
// =============================================

using AppHost.Tests.Infrastructure;

using Aspire.Hosting;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using MongoDB.Bson;
using MongoDB.Driver;

namespace AppHost.Tests;

/// <summary>
/// Full integration tests for the "seed-myblog-data" operator command (issue #260).
/// <para>
/// These tests require Docker because they boot the real Aspire host so that the handler's
/// closure-captured <c>mongo.Resource.ConnectionStringExpression.GetValueAsync()</c> can
/// resolve the live container's connection string.
/// </para>
/// </summary>
[Collection("MongoSeedIntegration")]
public sealed class MongoSeedDataIntegrationTests(ClearCommandAppFixture fixture)
{
	private const string CommandName = "seed-myblog-data";

	/// <summary>
	/// After the command runs, the blogposts collection contains at least 3 documents,
	/// including at least one unpublished draft.
	/// </summary>
	[Fact]
	public async Task SeedMyBlogData_Inserts_Expected_Documents_Into_BlogPosts_Collection()
	{
		// Arrange — drop and recreate an empty blogposts collection
		var client = new MongoClient(fixture.MongoConnectionString);
		var db = client.GetDatabase("myblog");
		await db.DropCollectionAsync("blogposts", TestContext.Current.CancellationToken);
		await db.CreateCollectionAsync("blogposts", cancellationToken: TestContext.Current.CancellationToken);

		var annotation = GetAnnotation();
		var ctx = MakeContext();

		// Act
		var result = await annotation.ExecuteCommand(ctx);

		// Assert
		result.Success.Should().BeTrue("the handler must succeed when MongoDB is reachable");

		var count = await db.GetCollection<BsonDocument>("blogposts")
			.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: TestContext.Current.CancellationToken);

		count.Should().BeGreaterThanOrEqualTo(3, "at least 3 seed documents must be inserted");

		var draftCount = await db.GetCollection<BsonDocument>("blogposts")
			.CountDocumentsAsync(
				Builders<BsonDocument>.Filter.Eq("IsPublished", false),
				cancellationToken: TestContext.Current.CancellationToken);

		draftCount.Should().BeGreaterThanOrEqualTo(1, "at least 1 document must be unpublished (draft)");
	}

	/// <summary>
	/// Two simultaneous seed attempts must not run together: exactly one proceeds and
	/// the other fails fast with operator-visible feedback.
	/// </summary>
	[Fact]
	public async Task SeedMyBlogData_Concurrent_Invocations_Allow_Only_One_Run()
	{
		// Arrange
		var client = new MongoClient(fixture.MongoConnectionString);
		var db = client.GetDatabase("myblog");
		await db.DropCollectionAsync("blogposts", TestContext.Current.CancellationToken);
		await db.CreateCollectionAsync("blogposts", cancellationToken: TestContext.Current.CancellationToken);

		var annotation = GetAnnotation();
		var ct = TestContext.Current.CancellationToken;
		var enteredCriticalSection = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var releaseCriticalSection = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		MongoDbResourceBuilderExtensions.SeedCommandAfterMutexAcquiredAsync = async cancellationToken =>
		{
			enteredCriticalSection.TrySetResult(true);
			await releaseCriticalSection.Task.WaitAsync(cancellationToken);
		};

		try
		{
			// Act — hold the first seed invocation inside the shared mutex, then trigger the
			// second invocation while the first is still in flight. This proves true overlap
			// deterministically instead of relying on Task.Run scheduler timing.
			var firstTask = annotation.ExecuteCommand(MakeContext());

			await enteredCriticalSection.Task.WaitAsync(ct);
			var secondResult = await annotation.ExecuteCommand(MakeContext());

			releaseCriticalSection.TrySetResult(true);
			var firstResult = await firstTask;
			var results = new[] { firstResult, secondResult };

			// Assert
			results.Count(static r => r.Success).Should().Be(1,
				"the semaphore should allow only one seed operation to run at a time");
			results.Count(static r => !r.Success).Should().Be(1,
				"the overlapping seed attempt should fail fast instead of queueing");
			results.Single(static r => !r.Success).Message.Should().Contain("already in progress",
				"the operator needs immediate feedback when another database operation is in flight");
		}
		finally
		{
			releaseCriticalSection.TrySetResult(true);
			MongoDbResourceBuilderExtensions.SeedCommandAfterMutexAcquiredAsync = null;
		}
	}

	/// <summary>
	/// When the database is completely empty (no collections at all), seeding must still
	/// create the blogposts collection and insert documents successfully.
	/// </summary>
	[Fact]
	public async Task SeedMyBlogData_Empty_Database_Results_In_BlogPosts_After_Seed()
	{
		// Arrange — drop the entire database so no collection exists
		var client = new MongoClient(fixture.MongoConnectionString);
		await client.DropDatabaseAsync("myblog", TestContext.Current.CancellationToken);

		var annotation = GetAnnotation();
		var ctx = MakeContext();

		// Act
		var result = await annotation.ExecuteCommand(ctx);

		// Assert
		result.Success.Should().BeTrue("seeding an empty database must succeed");

		var db = client.GetDatabase("myblog");
		var count = await db.GetCollection<BsonDocument>("blogposts")
			.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: TestContext.Current.CancellationToken);

		count.Should().BeGreaterThanOrEqualTo(1, "blogposts collection must have documents after seed");
	}


	// ---------------------------------------------------------------------------
	// Helpers
	// ---------------------------------------------------------------------------

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
