//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     BlogPostCacheService.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using System.Text.Json;

using MyBlog.Web.Data;

namespace MyBlog.Web.Infrastructure.Caching;

internal sealed class BlogPostCacheService(
	IMemoryCache localCache,
	IDistributedCache distributedCache) : IBlogPostCacheService
{
	private static readonly MemoryCacheEntryOptions LocalOpts =
		new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1));

	private static readonly DistributedCacheEntryOptions RedisOpts =
		new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

	private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

	public async ValueTask<IReadOnlyList<BlogPostDto>> GetOrFetchAllAsync(
		Func<Task<IReadOnlyList<BlogPostDto>>> fetch,
		CancellationToken ct = default)
	{
		// L1 hit (synchronous — no heap allocation)
		if (localCache.TryGetValue(BlogPostCacheKeys.All, out List<BlogPostDto>? cached) && cached is not null)
			return cached;

		// L2 hit
		var bytes = await distributedCache.GetAsync(BlogPostCacheKeys.All, ct);
		if (bytes is not null)
		{
			try
			{
				var fromRedis = JsonSerializer.Deserialize<List<BlogPostDto>>(bytes, JsonOpts);
				if (fromRedis is not null)
				{
					localCache.Set(BlogPostCacheKeys.All, fromRedis, LocalOpts);
					return fromRedis;
				}
			}
			catch (JsonException)
			{
				// Stale or corrupt bytes — remove and fall through to the DB
				await distributedCache.RemoveAsync(BlogPostCacheKeys.All, CancellationToken.None);
			}
		}

		// DB via caller-supplied fetch
		var result = await fetch();
		var list = result as List<BlogPostDto> ?? result.ToList();
		localCache.Set(BlogPostCacheKeys.All, list, LocalOpts);
		await distributedCache.SetAsync(
			BlogPostCacheKeys.All,
			JsonSerializer.SerializeToUtf8Bytes(list, JsonOpts),
			RedisOpts,
			ct);
		return result;
	}

	public async ValueTask<BlogPostDto?> GetOrFetchByIdAsync(
		Guid id,
		Func<Task<BlogPostDto?>> fetch,
		CancellationToken ct = default)
	{
		var key = BlogPostCacheKeys.ById(id);

		// L1 hit (synchronous — no heap allocation)
		if (localCache.TryGetValue(key, out BlogPostDto? cached) && cached is not null)
			return cached;

		// L2 hit
		var bytes = await distributedCache.GetAsync(key, ct);
		if (bytes is not null)
		{
			try
			{
				var dto = JsonSerializer.Deserialize<BlogPostDto>(bytes, JsonOpts);
				if (dto is not null)
				{
					localCache.Set(key, dto, LocalOpts);
					return dto;
				}
			}
			catch (JsonException)
			{
				// Stale or corrupt bytes — remove and fall through to the DB
				await distributedCache.RemoveAsync(key, CancellationToken.None);
			}
		}

		// DB via caller-supplied fetch
		var result = await fetch();
		if (result is null)
			return null;

		localCache.Set(key, result, LocalOpts);
		await distributedCache.SetAsync(
			key,
			JsonSerializer.SerializeToUtf8Bytes(result, JsonOpts),
			RedisOpts,
			ct);
		return result;
	}

	public async Task InvalidateAllAsync(CancellationToken ct = default)
	{
		localCache.Remove(BlogPostCacheKeys.All);
		// CancellationToken.None: the DB write already committed — must not be cancelled
		await distributedCache.RemoveAsync(BlogPostCacheKeys.All, CancellationToken.None);
	}

	public async Task InvalidateByIdAsync(Guid id, CancellationToken ct = default)
	{
		var key = BlogPostCacheKeys.ById(id);
		localCache.Remove(key);
		// CancellationToken.None: the DB write already committed — must not be cancelled
		await distributedCache.RemoveAsync(key, CancellationToken.None);
	}
}
