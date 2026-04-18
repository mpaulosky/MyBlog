//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     GetBlogPostsHandler.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using System.Text.Json;

namespace MyBlog.Web.Features.BlogPosts.List;

public sealed class GetBlogPostsHandler(
		IBlogPostRepository repo,
		IMemoryCache localCache,
		IDistributedCache distributedCache) : IRequestHandler<GetBlogPostsQuery, Result<IReadOnlyList<BlogPostDto>>>
{
	private static readonly MemoryCacheEntryOptions LocalOpts =
			new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1));
	private static readonly DistributedCacheEntryOptions RedisOpts =
			new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
	private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
	private const string CacheKey = "blog:all";

	public async Task<Result<IReadOnlyList<BlogPostDto>>> Handle(
			GetBlogPostsQuery request, CancellationToken ct)
	{
		try
		{
			if (localCache.TryGetValue(CacheKey, out List<BlogPostDto>? cached) && cached is not null)
				return Result.Ok<IReadOnlyList<BlogPostDto>>(cached);

			var bytes = await distributedCache.GetAsync(CacheKey, ct);
			if (bytes is not null)
			{
				var fromRedis = JsonSerializer.Deserialize<List<BlogPostDto>>(bytes, JsonOpts)!;
				localCache.Set(CacheKey, fromRedis, LocalOpts);
				return Result.Ok<IReadOnlyList<BlogPostDto>>(fromRedis);
			}

			var posts = await repo.GetAllAsync(ct);
			var dtos = posts.Select(p => p.ToDto()).ToList();
			localCache.Set(CacheKey, dtos, LocalOpts);
			await distributedCache.SetAsync(CacheKey,
					JsonSerializer.SerializeToUtf8Bytes(dtos, JsonOpts), RedisOpts, ct);
			return Result.Ok<IReadOnlyList<BlogPostDto>>(dtos);
		}
		catch (Exception ex)
		{
			return Result.Fail<IReadOnlyList<BlogPostDto>>(ex.Message);
		}
	}
}
