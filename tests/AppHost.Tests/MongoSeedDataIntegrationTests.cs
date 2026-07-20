// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     MongoSeedDataIntegrationTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : MyBlog
// Project Name :  AppHost.Tests
// =============================================

using AppHost.Infrastructure;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using MongoDB.Bson;
using MongoDB.Driver;

using MongoDbResourceBuilderExtensions = AppHost.MongoDbResourceBuilderExtensions;

namespace AppHost;

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
	[SkipInCIFact]
	public async Task SeedMyBlogData_Inserts_Expected_Documents_Into_BlogPosts_Collection()
	{
		// Arrange — drop and recreate an empty blogposts collection
		using var client = new MongoClient(fixture.MongoConnectionString);
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
	[SkipInCIFact]
	public async Task SeedMyBlogData_Concurrent_Invocations_Allow_Only_One_Run()
	{
		// Arrange
		using var client = new MongoClient(fixture.MongoConnectionString);
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
	[SkipInCIFact]
	public async Task SeedMyBlogData_Empty_Database_Results_In_BlogPosts_After_Seed()
	{
		// Arrange — drop the entire database so no collection exists
		using var client = new MongoClient(fixture.MongoConnectionString);
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

	/// <summary>
	/// Legacy collections from earlier schemas must be removed so the database converges on the
	/// canonical MyBlog collection set: blogposts + categories.
	/// </summary>
	[SkipInCIFact]
	public async Task SeedMyBlogData_Drops_Legacy_Posts_And_Tags_Collections()
	{
		// Arrange
		using var mongoClient = new MongoClient(fixture.MongoConnectionString);
		await mongoClient.DropDatabaseAsync("myblog", TestContext.Current.CancellationToken);
		var db = mongoClient.GetDatabase("myblog");
		await db.CreateCollectionAsync("posts", cancellationToken: TestContext.Current.CancellationToken);
		await db.CreateCollectionAsync("tags", cancellationToken: TestContext.Current.CancellationToken);
		await db.GetCollection<BsonDocument>("posts")
			.InsertOneAsync(new BsonDocument("n", 1), cancellationToken: TestContext.Current.CancellationToken);
		await db.GetCollection<BsonDocument>("tags")
			.InsertOneAsync(new BsonDocument("n", 1), cancellationToken: TestContext.Current.CancellationToken);

		var annotation = GetAnnotation();

		// Act
		var result = await annotation.ExecuteCommand(MakeContext());

		// Assert
		result.Success.Should().BeTrue("seeding should normalize the database before inserting canonical documents");
		var collectionNames = await (await db.ListCollectionNamesAsync(cancellationToken: TestContext.Current.CancellationToken))
			.ToListAsync(TestContext.Current.CancellationToken);
		collectionNames.Should().Contain("blogposts");
		collectionNames.Should().Contain("categories");
		collectionNames.Should().NotContain("posts", "legacy posts collection should be dropped during seed normalization");
		collectionNames.Should().NotContain("tags", "legacy tags collection should be dropped during seed normalization");
		result.Message.Should().Contain("dropped legacy collections: posts, tags");
	}

	/// <summary>
	/// The seed command must always upsert the canonical seven categories with their
	/// documented ObjectIds so downstream features can rely on stable category identities.
	/// </summary>
	[SkipInCIFact]
	public async Task SeedMyBlogData_Upserts_Seven_Canonical_Categories()
	{
		// Arrange
		using var mongoClient = new MongoClient(fixture.MongoConnectionString);
		await mongoClient.DropDatabaseAsync("myblog", TestContext.Current.CancellationToken);

		var annotation = GetAnnotation();
		var expectedCategories = new Dictionary<ObjectId, string>
		{
			[new ObjectId("677db927900ea4af1b500cab")] = "ASP.NET Core",
			[new ObjectId("677db927900ea4af1b500cac")] = "Blazor Server",
			[new ObjectId("677db9bd900ea4af1b500cad")] = "Blazor WebAssembly",
			[new ObjectId("677db9bd900ea4af1b500cae")] = "C#",
			[new ObjectId("677db9bd900ea4af1b500caf")] = "Entity Framework Core (EF Core)",
			[new ObjectId("677db9bd900ea4af1b500cb0")] = ".NET MAUI",
			[new ObjectId("677db9bd900ea4af1b500cb1")] = "Other",
		};

		// Act
		var result = await annotation.ExecuteCommand(MakeContext());

		// Assert
		result.Success.Should().BeTrue("seeding must succeed before canonical categories can be asserted");

		var db = mongoClient.GetDatabase("myblog");
		var seededCategories = await db.GetCollection<BsonDocument>("categories")
			.Find(FilterDefinition<BsonDocument>.Empty)
			.ToListAsync(TestContext.Current.CancellationToken);

		seededCategories.Should().HaveCount(7, "the AppHost seed should expose exactly the seven canonical categories");
		seededCategories.Should().OnlyContain(category => expectedCategories.ContainsKey(category["_id"].AsObjectId));
		seededCategories.Should().OnlyContain(category =>
			expectedCategories[category["_id"].AsObjectId] == category["Name"].AsString);
	}

	/// <summary>
	/// Seeded blog posts must point at the documented canonical category ObjectIds, so the
	/// sample data remains deterministic across reseeding.
	/// </summary>
	[SkipInCIFact]
	public async Task SeedMyBlogData_Assigns_BlogPosts_To_Expected_Canonical_Categories()
	{
		// Arrange
		using var mongoClient = new MongoClient(fixture.MongoConnectionString);
		await mongoClient.DropDatabaseAsync("myblog", TestContext.Current.CancellationToken);

		var annotation = GetAnnotation();
		var expectedCategoryByTitle = new Dictionary<string, ObjectId>(StringComparer.Ordinal)
		{
			["Welcome to MyBlog"] = new("677db9bd900ea4af1b500cb1"),
			["Getting Started with .NET Aspire"] = new("677db927900ea4af1b500cab"),
			["Draft: MongoDB Performance Tips"] = new("677db9bd900ea4af1b500cb1"),
		};

		// Act
		var result = await annotation.ExecuteCommand(MakeContext());

		// Assert
		result.Success.Should().BeTrue("seeding must succeed before seeded post/category links can be asserted");

		var db = mongoClient.GetDatabase("myblog");
		var seededPosts = await db.GetCollection<BsonDocument>("blogposts")
			.Find(FilterDefinition<BsonDocument>.Empty)
			.ToListAsync(TestContext.Current.CancellationToken);

		seededPosts.Should().HaveCount(expectedCategoryByTitle.Count,
			"the current AppHost seed should insert the documented three sample posts");
		seededPosts.Should().OnlyContain(post => expectedCategoryByTitle.ContainsKey(post["Title"].AsString));
		seededPosts.Should().OnlyContain(post =>
			expectedCategoryByTitle[post["Title"].AsString] == post["CategoryId"].AsObjectId);
	}

	/// <summary>
	/// After seeding succeeds, the running web app must be able to read the seeded posts
	/// through its real AppHost-wired MongoDB path.
	/// </summary>
	[SkipInCIFact]
	public async Task SeedMyBlogData_Makes_Seeded_Posts_Visible_On_The_Blog_Page()
	{
		// Arrange — start from a clean database, so any BlogPostCacheService L1/L2 cached
		// state cannot satisfy the assertions without a real MongoDB read.
		using var mongoClient = new MongoClient(fixture.MongoConnectionString);
		await mongoClient.DropDatabaseAsync("myblog", TestContext.Current.CancellationToken);

		var annotation = GetAnnotation();
		var endpoint = fixture.App.GetEndpoint("web", "https");
		using var handler = new HttpClientHandler
		{
			ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
		};
		using var webClient = new HttpClient(handler) { BaseAddress = endpoint };
		await WaitForWebReadyAsync(webClient, TimeSpan.FromMinutes(2));

		// Act
		var seedResult = await annotation.ExecuteCommand(MakeContext());

		// Assert — seeding succeeded.
		seedResult.Success.Should().BeTrue("seeding must succeed before the page can read MongoDB data");

		// Canonical proof #1 (cache-independent): documents are physically present in MongoDB,
		// confirming the real AppHost-wired seed path wrote to the live container.
		var db = mongoClient.GetDatabase("myblog");
		var seededCount = await db.GetCollection<BsonDocument>("blogposts")
			.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty,
				cancellationToken: TestContext.Current.CancellationToken);
		seededCount.Should().BeGreaterThanOrEqualTo(2,
			"at least 2 published posts must be present in MongoDB after seeding");

		// Canonical proof #2: the Web app reads the freshly seeded posts through the real
		// AppHost-wired MongoDB path.  With [StreamRendering] the server runs
		// OnInitializedAsync during SSR pre-render and streams the final rendered DOM
		// (including <td>@post.Title</td> rows) as a <template blazor-component-id="…">
		// block inside the HTTP response body — so the seed title appears verbatim in a
		// plain GetAsync call.
		//
		// PollBlogUntilSeededTitleVisibleAsync throws TimeoutException (with a sanitized
		// last-status / last-body summary) if the title never appears, preventing a stale
		// cached empty-list from passing silently.  The 2-minute window exceeds the
		// BlogPostCacheService L1 TTL (1 min) so any pre-reseed cached state can expire,
		// and the handler re-queries MongoDB, making the test order-independent.
		const string seededTitle = "Welcome to MyBlog";
		var pageHtml = await PollBlogUntilSeededTitleVisibleAsync(
			webClient,
			seededTitle: seededTitle,
			timeout: TimeSpan.FromMinutes(2),
			TestContext.Current.CancellationToken);

		pageHtml.Should().Contain(seededTitle,
			"the seeded post title must appear in the /blog SSR output — proving Web read from MongoDB");
		pageHtml.Should().NotContain("An unexpected error occurred.",
			"a MongoDB connectivity regression must surface as a failing test, not a silent error page");
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
		Arguments = null!
	};

	private static async Task WaitForWebReadyAsync(HttpClient client, TimeSpan timeout)
	{
		using var cts = new CancellationTokenSource(timeout);
		client.Timeout = timeout;

		while (true)
		{
			try
			{
				using var response = await client.GetAsync(new Uri("/alive", UriKind.Relative), cts.Token);
				if (response.IsSuccessStatusCode)
					return;
			}
			catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
			{
				throw new TimeoutException("Web app did not become ready before the MongoDB runtime connectivity check.");
			}
			catch (HttpRequestException) { }

			try
			{
				await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
			}
			catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
			{
				throw new TimeoutException("Web app did not become ready before the MongoDB runtime connectivity check.");
			}
		}
	}

	/// <summary>
	/// Polls GET /blog until the response body contains <paramref name="seededTitle"/>,
	/// or until <paramref name="timeout"/> elapses.
	/// <para>
	/// Returns the full response body once the title is found — the caller can assert
	/// directly on it as positive proof that the Web app read the seeded document from
	/// MongoDB through the real AppHost-wired path.
	/// </para>
	/// <para>
	/// If the title is never observed within <paramref name="timeout"/>, a
	/// <see cref="TimeoutException"/> is thrown with a sanitized summary of the last
	/// HTTP status and the first 400 characters of the last response body (no secrets).
	/// This makes test failures immediately diagnosable and prevents a stale cached
	/// empty-list from causing the test to pass silently.
	/// </para>
	/// </summary>
	private static async Task<string> PollBlogUntilSeededTitleVisibleAsync(
		HttpClient client,
		string seededTitle,
		TimeSpan timeout,
		CancellationToken ct)
	{
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		cts.CancelAfter(timeout);

		var lastStatusCode = HttpStatusCode.ServiceUnavailable;
		var lastBodySummary = "(no response received)";

		while (!cts.Token.IsCancellationRequested)
		{
			try
			{
				using var response = await client.GetAsync(new Uri("/blog", UriKind.Relative), cts.Token);
				lastStatusCode = response.StatusCode;
				var body = await response.Content.ReadAsStringAsync(cts.Token);
				lastBodySummary = body.Length > 400 ? body[..400] + "…(truncated)" : body;

				if (body.Contains(seededTitle, StringComparison.Ordinal))
					return body;
			}
			catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
			{
				break;
			}
			catch (HttpRequestException) { }

			try
			{
				await Task.Delay(TimeSpan.FromSeconds(3), cts.Token);
			}
			catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
			{
				break;
			}
		}

		throw new TimeoutException(
			$"Timed out after {timeout.TotalMinutes:F0} min waiting for '{seededTitle}' on /blog. " +
			$"Last HTTP status: {(int)lastStatusCode} {lastStatusCode}. " +
			$"Last body summary: {lastBodySummary}");
	}
}
