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
		var db = await DropAndSeedAsync(new Dictionary<string, int>
		{
			["posts"] = 5,
			["comments"] = 3,
		});

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
		await DropAndSeedAsync(new Dictionary<string, int>
		{
			["posts"] = 4,
			["tags"] = 2,
		});

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
		await DropAndSeedAsync(new Dictionary<string, int>
		{
			["posts"] = 1,
			["empty-collection"] = 0,
		});

		var annotation = GetAnnotation();
		var ctx = MakeContext();

		// Act
		var result = await annotation.ExecuteCommand(ctx);

		// Assert
		result.Success.Should().BeTrue();
		result.Message.Should().Contain("empty-collection: 0",
			"a collection that was already empty must appear in the result with count 0");
	}

	// ---------------------------------------------------------------------------
	// Helpers
	// ---------------------------------------------------------------------------

	private async Task<IMongoDatabase> DropAndSeedAsync(Dictionary<string, int> collections)
	{
		var client = new MongoClient(fixture.MongoConnectionString);
		client.DropDatabase("myblog");
		var db = client.GetDatabase("myblog");

		foreach (var (name, count) in collections)
		{
			await db.CreateCollectionAsync(name);

			if (count > 0)
			{
				var docs = Enumerable
					.Range(0, count)
					.Select(i => new BsonDocument("n", i))
					.ToList();

				await db.GetCollection<BsonDocument>(name).InsertManyAsync(docs);
			}
		}

		return db;
	}

	private ResourceCommandAnnotation GetAnnotation()
	{
		var mongoResource = fixture.Builder.Resources.Single(static r => r.Name == "mongodb");

		return mongoResource.Annotations
			.OfType<ResourceCommandAnnotation>()
			.Single(static a => a.Name == CommandName);
	}

	private ExecuteCommandContext MakeContext() => new()
	{
		ResourceName = "mongodb",
		ServiceProvider = new ServiceCollection().BuildServiceProvider(),
		Logger = NullLogger.Instance,
		CancellationToken = TestContext.Current.CancellationToken,
	};
}
