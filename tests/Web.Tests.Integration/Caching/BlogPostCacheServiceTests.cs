//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     BlogPostCacheServiceTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests.Integration
//=======================================================

using Web.Infrastructure;

namespace Web.Caching;

[Collection("RedisCaching")]
public sealed class BlogPostCacheServiceTests(RedisFixture fixture)
{
	// ------------------------------------------------------------------ helpers

	private static BlogPostDto MakeDto(string title = "Test Post") =>
		new(Guid.NewGuid(), title, "Content", "Author", DateTime.UtcNow, null, true);

	// ------------------------------------------------------------------ tests

	[Fact]
	public async Task GetOrFetchAllAsync_populates_Redis_on_cache_miss()
	{
		// Arrange — ensure clean Redis state (shared container; previous tests may have populated blog:all)
		var ct = TestContext.Current.CancellationToken;
		await fixture.CreateCacheService().InvalidateAllAsync(ct);

		var svc1 = fixture.CreateCacheService();
		var dto = MakeDto("Redis Test Post");
		IReadOnlyList<BlogPostDto> dbResult = new List<BlogPostDto> { dto };

		var fetch1 = Substitute.For<Func<Task<IReadOnlyList<BlogPostDto>>>>();
		fetch1().Returns(Task.FromResult(dbResult));

		// Act — L1 miss → Redis miss → delegate fired → writes to Redis
		var result1 = await svc1.GetOrFetchAllAsync(fetch1, ct);

		// Assert
		result1.Should().HaveCount(1);
		result1[0].Title.Should().Be("Redis Test Post");
		fetch1.ReceivedCalls().Should().HaveCount(1);

		// Arrange — service #2: fresh L1 (cold), same Redis container (warm)
		var svc2 = fixture.CreateCacheService();

		var fetch2 = Substitute.For<Func<Task<IReadOnlyList<BlogPostDto>>>>();
		fetch2().Returns(Task.FromResult(dbResult));

		// Act — L1 miss → Redis HIT → delegate NOT fired
		var result2 = await svc2.GetOrFetchAllAsync(fetch2, ct);

		// Assert — Redis served the data without calling the DB delegate
		result2.Should().HaveCount(1);
		result2[0].Title.Should().Be("Redis Test Post");
		fetch2.ReceivedCalls().Should().BeEmpty();
	}

	[Fact]
	public async Task GetOrFetchByIdAsync_populates_Redis_on_cache_miss()
	{
		// Arrange — service #1 with cold L1
		var ct = TestContext.Current.CancellationToken;
		var svc1 = fixture.CreateCacheService();
		var dto = MakeDto("By-Id Post");
		var id = dto.Id;

		var fetch1 = Substitute.For<Func<Task<BlogPostDto?>>>();
		fetch1().Returns(Task.FromResult<BlogPostDto?>(dto));

		// Act — L1 miss → Redis miss → delegate fired → writes to Redis
		var result1 = await svc1.GetOrFetchByIdAsync(id, fetch1, ct);

		// Assert
		result1.Should().NotBeNull();
		result1!.Title.Should().Be("By-Id Post");
		fetch1.ReceivedCalls().Should().HaveCount(1);

		// Arrange — service #2: fresh L1, same Redis (now contains the key)
		var svc2 = fixture.CreateCacheService();

		var fetch2 = Substitute.For<Func<Task<BlogPostDto?>>>();
		fetch2().Returns(Task.FromResult<BlogPostDto?>(dto));

		// Act — L1 miss → Redis HIT → delegate NOT fired
		var result2 = await svc2.GetOrFetchByIdAsync(id, fetch2, ct);

		// Assert
		result2.Should().NotBeNull();
		result2!.Title.Should().Be("By-Id Post");
		fetch2.ReceivedCalls().Should().BeEmpty();
	}

	[Fact]
	public async Task InvalidateAllAsync_removes_all_entries_from_Redis()
	{
		// Arrange — ensure clean state then populate Redis via service #1
		var ct = TestContext.Current.CancellationToken;
		await fixture.CreateCacheService().InvalidateAllAsync(ct);

		var svc1 = fixture.CreateCacheService();
		var dto = MakeDto("Post To Invalidate");
		IReadOnlyList<BlogPostDto> dbResult = new List<BlogPostDto> { dto };

		var populate = Substitute.For<Func<Task<IReadOnlyList<BlogPostDto>>>>();
		populate().Returns(Task.FromResult(dbResult));

		await svc1.GetOrFetchAllAsync(populate, ct);
		populate.ReceivedCalls().Should().HaveCount(1); // confirm it went to DB

		// Act — invalidate through svc1 (removes from L1 and Redis)
		await svc1.InvalidateAllAsync(ct);

		// Arrange — service #2: fresh L1 + Redis now evicted
		var svc2 = fixture.CreateCacheService();

		var fetchAfterEviction = Substitute.For<Func<Task<IReadOnlyList<BlogPostDto>>>>();
		fetchAfterEviction().Returns(Task.FromResult(dbResult));

		// Act — L1 miss → Redis miss (evicted) → delegate MUST fire
		var resultAfter = await svc2.GetOrFetchAllAsync(fetchAfterEviction, ct);

		// Assert — delegate was called because Redis was truly evicted
		resultAfter.Should().HaveCount(1);
		fetchAfterEviction.ReceivedCalls().Should().HaveCount(1);
	}
}
