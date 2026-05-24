//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     BlogPostCacheServiceTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using System.Text.Json;

using Microsoft.Extensions.Caching.Memory;

namespace Web.Infrastructure.Caching;

public class BlogPostCacheServiceTests : IDisposable
{
	private readonly MemoryCache _realLocalCache = new(new MemoryCacheOptions());
	private readonly IDistributedCache _distributedCache = Substitute.For<IDistributedCache>();
	private readonly BlogPostCacheService _sut;

	public BlogPostCacheServiceTests()
	{
		_sut = new BlogPostCacheService(_realLocalCache, _distributedCache);
	}

	public void Dispose() => _realLocalCache.Dispose();

	private static readonly JsonSerializerOptions JsonOpts = BlogPostCacheService.JsonOpts;

	private static List<BlogPostDto> MakeDtos() =>
	[
		new(ObjectId.GenerateNewId(), "Title1", "Content1", string.Empty, "Author1", string.Empty, [], DateTime.UtcNow, null, false, ObjectId.GenerateNewId()),
		new(ObjectId.GenerateNewId(), "Title2", "Content2", string.Empty, "Author2", string.Empty, [], DateTime.UtcNow, null, true, null),
	];

	// ── GetOrFetchAllAsync ────────────────────────────────────────────────

	[Fact]
	public async Task GetOrFetchAllAsync_L1Hit_ReturnsCachedListWithoutDistributedCall()
	{
		// Arrange
		var cachedList = MakeDtos();
		_realLocalCache.Set(BlogPostCacheKeys.All, cachedList);

		var fetchCalled = false;
		Task<IReadOnlyList<BlogPostDto>> fetch() { fetchCalled = true; return Task.FromResult<IReadOnlyList<BlogPostDto>>([]); }

		// Act
		var result = await _sut.GetOrFetchAllAsync(fetch, CancellationToken.None);

		// Assert
		result.Should().HaveCount(2);
		result.Should().BeEquivalentTo(cachedList);
		fetchCalled.Should().BeFalse();
		await _distributedCache.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetOrFetchAllAsync_L2Hit_DeserializesAndPopulatesL1()
	{
		// Arrange — no L1 entry, L2 has valid bytes
		var dtos = MakeDtos();
		var bytes = JsonSerializer.SerializeToUtf8Bytes(dtos, JsonOpts);
		_distributedCache.GetAsync(BlogPostCacheKeys.All, Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<byte[]?>(bytes));

		// Act
		var result = await _sut.GetOrFetchAllAsync(
			() => Task.FromResult<IReadOnlyList<BlogPostDto>>([]), CancellationToken.None);

		// Assert
		result.Should().HaveCount(2);
		result[0].Id.Should().Be(dtos[0].Id);
		result[0].CategoryId.Should().Be(dtos[0].CategoryId);
		_realLocalCache.TryGetValue(BlogPostCacheKeys.All, out List<BlogPostDto>? l1Val).Should().BeTrue();
		l1Val.Should().HaveCount(2);
	}

	[Fact]
	public async Task GetOrFetchAllAsync_L2JsonCorrupt_RemovesAndFallsThroughToFetch()
	{
		// Arrange
		var corruptBytes = "{ not valid json !!!"u8.ToArray();
		_distributedCache.GetAsync(BlogPostCacheKeys.All, Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<byte[]?>(corruptBytes));
		_distributedCache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);
		_distributedCache.SetAsync(
			Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);

		var expected = MakeDtos();
		var fetchCalled = false;
		Task<IReadOnlyList<BlogPostDto>> fetch()
		{
			fetchCalled = true;
			return Task.FromResult<IReadOnlyList<BlogPostDto>>(expected);
		}

		// Act
		var result = await _sut.GetOrFetchAllAsync(fetch, CancellationToken.None);

		// Assert
		fetchCalled.Should().BeTrue();
		result.Should().HaveCount(2);
		await _distributedCache.Received().RemoveAsync(BlogPostCacheKeys.All, CancellationToken.None);
	}

	[Fact]
	public async Task GetOrFetchAllAsync_FullMiss_FetchesAndPopulatesBothTiers()
	{
		// Arrange — nothing in L1 or L2
		_distributedCache.GetAsync(BlogPostCacheKeys.All, Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<byte[]?>(null));
		_distributedCache.SetAsync(
			Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);

		var dtos = MakeDtos();

		// Act
		var result = await _sut.GetOrFetchAllAsync(
			() => Task.FromResult<IReadOnlyList<BlogPostDto>>(dtos), CancellationToken.None);

		// Assert
		result.Should().HaveCount(2);
		_realLocalCache.TryGetValue(BlogPostCacheKeys.All, out List<BlogPostDto>? l1Val).Should().BeTrue();
		l1Val.Should().HaveCount(2);
		await _distributedCache.Received().SetAsync(
			BlogPostCacheKeys.All,
			Arg.Any<byte[]>(),
			Arg.Any<DistributedCacheEntryOptions>(),
			Arg.Any<CancellationToken>());
	}

	// ── GetOrFetchByIdAsync ───────────────────────────────────────────────

	[Fact]
	public async Task GetOrFetchByIdAsync_L1Hit_ReturnsCachedDtoWithoutDistributedCall()
	{
		// Arrange
		var id = ObjectId.GenerateNewId();
		var key = BlogPostCacheKeys.ById(id);
		var dto = new BlogPostDto(id, "T", "C", string.Empty, "A", string.Empty, [], DateTime.UtcNow, null, false, null);
		_realLocalCache.Set(key, dto);

		// Act
		var result = await _sut.GetOrFetchByIdAsync(id, () => Task.FromResult<BlogPostDto?>(null), CancellationToken.None);

		// Assert
		result.Should().NotBeNull();
		result!.Id.Should().Be(id);
		await _distributedCache.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetOrFetchByIdAsync_L2Hit_DeserializesAndPopulatesL1()
	{
		// Arrange
		var id = ObjectId.GenerateNewId();
		var key = BlogPostCacheKeys.ById(id);
		var dto = new BlogPostDto(id, "T", "C", string.Empty, "A", string.Empty, [], DateTime.UtcNow, null, false, null);
		var bytes = JsonSerializer.SerializeToUtf8Bytes(dto, JsonOpts);
		_distributedCache.GetAsync(key, Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<byte[]?>(bytes));

		// Act
		var result = await _sut.GetOrFetchByIdAsync(id, () => Task.FromResult<BlogPostDto?>(null), CancellationToken.None);

		// Assert
		result.Should().NotBeNull();
		result!.Id.Should().Be(id);
		_realLocalCache.TryGetValue(key, out BlogPostDto? l1Val).Should().BeTrue();
		l1Val!.Id.Should().Be(id);
	}

	[Fact]
	public async Task GetOrFetchByIdAsync_L2JsonCorrupt_RemovesAndFallsThroughToFetch()
	{
		// Arrange
		var id = ObjectId.GenerateNewId();
		var key = BlogPostCacheKeys.ById(id);
		var corruptBytes = "{ not valid json !!!"u8.ToArray();
		_distributedCache.GetAsync(key, Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<byte[]?>(corruptBytes));
		_distributedCache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);
		_distributedCache.SetAsync(
			Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);

		var dto = new BlogPostDto(id, "T", "C", string.Empty, "A", string.Empty, [], DateTime.UtcNow, null, false, null);
		var fetchCalled = false;
		Task<BlogPostDto?> fetch() { fetchCalled = true; return Task.FromResult<BlogPostDto?>(dto); }

		// Act
		var result = await _sut.GetOrFetchByIdAsync(id, fetch, CancellationToken.None);

		// Assert
		fetchCalled.Should().BeTrue();
		result!.Id.Should().Be(id);
		await _distributedCache.Received().RemoveAsync(key, CancellationToken.None);
	}

	[Fact]
	public async Task GetOrFetchByIdAsync_FullMiss_FetchesAndPopulatesBothTiers()
	{
		// Arrange
		var id = ObjectId.GenerateNewId();
		var key = BlogPostCacheKeys.ById(id);
		_distributedCache.GetAsync(key, Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<byte[]?>(null));
		_distributedCache.SetAsync(
			Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);

		var dto = new BlogPostDto(id, "T", "C", string.Empty, "A", string.Empty, [], DateTime.UtcNow, null, false, null);

		// Act
		var result = await _sut.GetOrFetchByIdAsync(id, () => Task.FromResult<BlogPostDto?>(dto), CancellationToken.None);

		// Assert
		result!.Id.Should().Be(id);
		_realLocalCache.TryGetValue(key, out BlogPostDto? l1Val).Should().BeTrue();
		l1Val!.Id.Should().Be(id);
		await _distributedCache.Received().SetAsync(
			key, Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetOrFetchByIdAsync_FetchReturnsNull_ReturnsNull()
	{
		// Arrange
		var id = ObjectId.GenerateNewId();
		var key = BlogPostCacheKeys.ById(id);
		_distributedCache.GetAsync(key, Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<byte[]?>(null));

		// Act
		var result = await _sut.GetOrFetchByIdAsync(id, () => Task.FromResult<BlogPostDto?>(null), CancellationToken.None);

		// Assert
		result.Should().BeNull();
		await _distributedCache.DidNotReceive().SetAsync(
			Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>());
	}

	// ── InvalidateAllAsync ────────────────────────────────────────────────

	[Fact]
	public async Task InvalidateAllAsync_RemovesBothCacheTiers()
	{
		// Arrange — populate L1 so we can verify removal
		var dtos = MakeDtos();
		_realLocalCache.Set(BlogPostCacheKeys.All, dtos);
		_distributedCache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);

		// Act
		await _sut.InvalidateAllAsync(CancellationToken.None);

		// Assert
		_realLocalCache.TryGetValue(BlogPostCacheKeys.All, out List<BlogPostDto>? _).Should().BeFalse();
		await _distributedCache.Received(1).RemoveAsync(BlogPostCacheKeys.All, CancellationToken.None);
	}

	// ── InvalidateByIdAsync ───────────────────────────────────────────────

	[Fact]
	public async Task InvalidateByIdAsync_RemovesByKeyFromBothTiers()
	{
		// Arrange — populate L1 so we can verify removal
		var id = ObjectId.GenerateNewId();
		var key = BlogPostCacheKeys.ById(id);
		var dto = new BlogPostDto(id, "T", "C", string.Empty, "A", string.Empty, [], DateTime.UtcNow, null, false, null);
		_realLocalCache.Set(key, dto);
		_distributedCache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);

		// Act
		await _sut.InvalidateByIdAsync(id, CancellationToken.None);

		// Assert
		_realLocalCache.TryGetValue(key, out BlogPostDto? _).Should().BeFalse();
		await _distributedCache.Received(1).RemoveAsync(key, CancellationToken.None);
	}
}
