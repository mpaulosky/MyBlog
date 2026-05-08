// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     MongoShowStatsIntegrationTests.cs
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
/// Full integration tests for the "show-myblog-stats" operator command (issue #261).
/// <para>
/// These tests require Docker because they boot the real Aspire host so that the handler's
/// closure-captured <c>mongo.Resource.ConnectionStringExpression.GetValueAsync()</c> can
/// resolve the live container's connection string.
/// </para>
/// </summary>
[Collection("MongoStatsIntegration")]
public sealed class MongoShowStatsIntegrationTests(ClearCommandAppFixture fixture)
{
	private const string CommandName = "show-myblog-stats";

	/// <summary>
	/// When at least one collection with documents exists, the command returns success and
	/// reports the collection count in its message.
	/// </summary>
	[Fact]
	public async Task ShowMyBlogStats_Returns_Collection_Names_And_Counts_In_Markdown()
	{
		// Arrange — drop db, then insert documents into blogposts
		await PrepareAsync(blogPostCount: 1);

		var annotation = GetAnnotation();
		var ctx = MakeContext();

		// Act
		var result = await annotation.ExecuteCommand(ctx);

		// Assert
		result.Success.Should().BeTrue("the handler must succeed when MongoDB is reachable");
		result.Message.Should().Contain("collection(s)",
			"the success message must report the number of collections found");
	}

	/// <summary>
	/// When the database is completely empty (no user collections), the command must still
	/// succeed — an empty database is not an error condition.
	/// </summary>
	[Fact]
	public async Task ShowMyBlogStats_Empty_Database_Returns_No_Collections_Found()
	{
		// Arrange — drop the entire myblog database so no collection exists
		await PrepareAsync(blogPostCount: 0);

		var annotation = GetAnnotation();
		var ctx = MakeContext();

		// Act
		var result = await annotation.ExecuteCommand(ctx);

		// Assert
		result.Success.Should().BeTrue("an empty database must still return success — no collections is not an error");
		result.Message.Should().Contain("0 collection(s)",
			"the message must indicate zero collections were found in the empty database");
	}

	/// <summary>
	/// Two simultaneous stats attempts must not run together: exactly one proceeds and
	/// the other fails fast with operator-visible feedback.
	/// </summary>
	[Fact]
	public async Task ShowMyBlogStats_Concurrent_Invocations_Allow_Only_One_Run()
	{
		// Arrange
		await PrepareAsync(blogPostCount: 0);

		var annotation = GetAnnotation();

		// Act — fire two concurrent stats operations
		var firstTask = annotation.ExecuteCommand(MakeContext());
		var secondTask = annotation.ExecuteCommand(MakeContext());
		var results = await Task.WhenAll(firstTask, secondTask);

		// Assert
		results.Count(static r => r.Success).Should().Be(1,
			"the semaphore should allow only one stats operation to run at a time");
		results.Count(static r => !r.Success).Should().Be(1,
			"the overlapping stats attempt should fail fast instead of queueing");
		results.Single(static r => !r.Success).Message.Should().Contain("already in progress",
			"the operator needs immediate feedback when another database operation is in flight");
	}


	// ---------------------------------------------------------------------------
	// Helpers
	// ---------------------------------------------------------------------------

	private async Task PrepareAsync(int blogPostCount = 0)
	{
		var client = new MongoClient(fixture.MongoConnectionString);
		await client.DropDatabaseAsync("myblog", TestContext.Current.CancellationToken);
		if (blogPostCount > 0)
		{
			var db = client.GetDatabase("myblog");
			var col = db.GetCollection<BsonDocument>("blogposts");
			var docs = Enumerable.Range(0, blogPostCount)
				.Select(i => new BsonDocument("n", i))
				.ToList();
			await col.InsertManyAsync(docs, cancellationToken: TestContext.Current.CancellationToken);
		}
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
