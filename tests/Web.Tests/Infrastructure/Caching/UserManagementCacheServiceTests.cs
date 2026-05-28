//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     UserManagementCacheServiceTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using System.Text.Json;

using MyBlog.Web.Features.UserManagement;

namespace Web.Infrastructure.Caching;

public sealed class UserManagementCacheServiceTests : IDisposable
{
	private readonly MemoryCache _localCache = new(new MemoryCacheOptions());
	private readonly IDistributedCache _distributedCache = Substitute.For<IDistributedCache>();
	private readonly UserManagementCacheService _sut;

	private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

	public UserManagementCacheServiceTests()
	{
		_sut = new UserManagementCacheService(_localCache, _distributedCache);
	}

	public void Dispose()
	{
		_localCache.Dispose();
	}

	private static List<UserWithRolesDto> MakeUsers() =>
	[
		new("user-1", "alice@example.com", "Alice", ["Admin"]),
		new("user-2", "bob@example.com", "Bob", ["Author"]),
	];

	private static List<RoleDto> MakeRoles() =>
	[
		new("role-1", "Admin"),
		new("role-2", "Author"),
	];

	// ── GetOrFetchUsersAsync ──────────────────────────────────────────────

	[Fact]
	public async Task GetOrFetchUsersAsync_L1Hit_ReturnsCachedListWithoutDistributedCall()
	{
		// Arrange
		var cachedList = MakeUsers();
		_localCache.Set(UserManagementCacheKeys.AllUsers, cachedList);

		var fetchCalled = false;
		Task<IReadOnlyList<UserWithRolesDto>> fetch() { fetchCalled = true; return Task.FromResult<IReadOnlyList<UserWithRolesDto>>([]); }

		// Act
		var result = await _sut.GetOrFetchUsersAsync(fetch, CancellationToken.None);

		// Assert
		result.Should().HaveCount(2);
		fetchCalled.Should().BeFalse();
		await _distributedCache.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetOrFetchUsersAsync_L2Hit_DeserializesAndPopulatesL1()
	{
		// Arrange
		var users = MakeUsers();
		var bytes = JsonSerializer.SerializeToUtf8Bytes(users, JsonOpts);
		_distributedCache.GetAsync(UserManagementCacheKeys.AllUsers, Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<byte[]?>(bytes));

		var fetchCalled = false;
		Task<IReadOnlyList<UserWithRolesDto>> fetch() { fetchCalled = true; return Task.FromResult<IReadOnlyList<UserWithRolesDto>>([]); }

		// Act
		var result = await _sut.GetOrFetchUsersAsync(fetch, CancellationToken.None);

		// Assert
		result.Should().HaveCount(2);
		fetchCalled.Should().BeFalse();
		_localCache.TryGetValue(UserManagementCacheKeys.AllUsers, out List<UserWithRolesDto>? l1).Should().BeTrue();
		l1.Should().HaveCount(2);
	}

	[Fact]
	public async Task GetOrFetchUsersAsync_CacheMiss_CallsFetchAndPopulatesBothTiers()
	{
		// Arrange
		_distributedCache.GetAsync(UserManagementCacheKeys.AllUsers, Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<byte[]?>(null));
		_distributedCache.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);

		var users = MakeUsers();
		var fetchCalled = false;
		Task<IReadOnlyList<UserWithRolesDto>> fetch() { fetchCalled = true; return Task.FromResult<IReadOnlyList<UserWithRolesDto>>(users); }

		// Act
		var result = await _sut.GetOrFetchUsersAsync(fetch, CancellationToken.None);

		// Assert
		result.Should().HaveCount(2);
		fetchCalled.Should().BeTrue();
		await _distributedCache.Received(1).SetAsync(
			UserManagementCacheKeys.AllUsers, Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetOrFetchUsersAsync_CorruptL2Bytes_RemovesAndFallsThrough()
	{
		// Arrange — corrupt bytes that will fail JSON deserialization
		_distributedCache.GetAsync(UserManagementCacheKeys.AllUsers, Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<byte[]?>(new byte[] { 0xFF, 0xFE }));
		_distributedCache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);
		_distributedCache.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);

		var users = MakeUsers();
		Task<IReadOnlyList<UserWithRolesDto>> fetch() => Task.FromResult<IReadOnlyList<UserWithRolesDto>>(users);

		// Act
		var result = await _sut.GetOrFetchUsersAsync(fetch, CancellationToken.None);

		// Assert
		result.Should().HaveCount(2);
		await _distributedCache.Received(1).RemoveAsync(UserManagementCacheKeys.AllUsers, CancellationToken.None);
	}

	// ── GetOrFetchRolesAsync ──────────────────────────────────────────────

	[Fact]
	public async Task GetOrFetchRolesAsync_L1Hit_ReturnsCachedListWithoutDistributedCall()
	{
		// Arrange
		var roles = MakeRoles();
		_localCache.Set(UserManagementCacheKeys.AllRoles, roles);

		var fetchCalled = false;
		Task<IReadOnlyList<RoleDto>> fetch() { fetchCalled = true; return Task.FromResult<IReadOnlyList<RoleDto>>([]); }

		// Act
		var result = await _sut.GetOrFetchRolesAsync(fetch, CancellationToken.None);

		// Assert
		result.Should().HaveCount(2);
		fetchCalled.Should().BeFalse();
		await _distributedCache.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetOrFetchRolesAsync_L2Hit_DeserializesAndPopulatesL1()
	{
		// Arrange
		var roles = MakeRoles();
		var bytes = JsonSerializer.SerializeToUtf8Bytes(roles, JsonOpts);
		_distributedCache.GetAsync(UserManagementCacheKeys.AllRoles, Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<byte[]?>(bytes));

		var fetchCalled = false;
		Task<IReadOnlyList<RoleDto>> fetch() { fetchCalled = true; return Task.FromResult<IReadOnlyList<RoleDto>>([]); }

		// Act
		var result = await _sut.GetOrFetchRolesAsync(fetch, CancellationToken.None);

		// Assert
		result.Should().HaveCount(2);
		fetchCalled.Should().BeFalse();
	}

	[Fact]
	public async Task GetOrFetchRolesAsync_CacheMiss_CallsFetchAndPopulatesBothTiers()
	{
		// Arrange
		_distributedCache.GetAsync(UserManagementCacheKeys.AllRoles, Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<byte[]?>(null));
		_distributedCache.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);

		var roles = MakeRoles();
		var fetchCalled = false;
		Task<IReadOnlyList<RoleDto>> fetch() { fetchCalled = true; return Task.FromResult<IReadOnlyList<RoleDto>>(roles); }

		// Act
		var result = await _sut.GetOrFetchRolesAsync(fetch, CancellationToken.None);

		// Assert
		result.Should().HaveCount(2);
		fetchCalled.Should().BeTrue();
		await _distributedCache.Received(1).SetAsync(
			UserManagementCacheKeys.AllRoles, Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetOrFetchRolesAsync_CorruptL2Bytes_RemovesAndFallsThrough()
	{
		// Arrange
		_distributedCache.GetAsync(UserManagementCacheKeys.AllRoles, Arg.Any<CancellationToken>())
			.Returns(Task.FromResult<byte[]?>(new byte[] { 0xFF, 0xFE }));
		_distributedCache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);
		_distributedCache.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);

		var roles = MakeRoles();
		Task<IReadOnlyList<RoleDto>> fetch() => Task.FromResult<IReadOnlyList<RoleDto>>(roles);

		// Act
		var result = await _sut.GetOrFetchRolesAsync(fetch, CancellationToken.None);

		// Assert
		result.Should().HaveCount(2);
		await _distributedCache.Received(1).RemoveAsync(UserManagementCacheKeys.AllRoles, CancellationToken.None);
	}

	// ── InvalidateUsersAsync ──────────────────────────────────────────────

	[Fact]
	public async Task InvalidateUsersAsync_RemovesBothCacheTiers()
	{
		// Arrange
		_localCache.Set(UserManagementCacheKeys.AllUsers, MakeUsers());
		_distributedCache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);

		// Act
		await _sut.InvalidateUsersAsync(CancellationToken.None);

		// Assert
		_localCache.TryGetValue(UserManagementCacheKeys.AllUsers, out List<UserWithRolesDto>? _).Should().BeFalse();
		await _distributedCache.Received(1).RemoveAsync(UserManagementCacheKeys.AllUsers, CancellationToken.None);
	}

	// ── InvalidateRolesAsync ──────────────────────────────────────────────

	[Fact]
	public async Task InvalidateRolesAsync_RemovesBothCacheTiers()
	{
		// Arrange
		_localCache.Set(UserManagementCacheKeys.AllRoles, MakeRoles());
		_distributedCache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);

		// Act
		await _sut.InvalidateRolesAsync(CancellationToken.None);

		// Assert
		_localCache.TryGetValue(UserManagementCacheKeys.AllRoles, out List<RoleDto>? _).Should().BeFalse();
		await _distributedCache.Received(1).RemoveAsync(UserManagementCacheKeys.AllRoles, CancellationToken.None);
	}
}
