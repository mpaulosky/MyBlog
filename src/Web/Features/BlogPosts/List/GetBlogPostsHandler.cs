using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using MyBlog.Domain.Interfaces;
using MyBlog.Web.Data;
using MyBlog.Domain.Common;

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
                return Result<IReadOnlyList<BlogPostDto>>.Success(cached);

            var bytes = await distributedCache.GetAsync(CacheKey, ct);
            if (bytes is not null)
            {
                var fromRedis = JsonSerializer.Deserialize<List<BlogPostDto>>(bytes, JsonOpts)!;
                localCache.Set(CacheKey, fromRedis, LocalOpts);
                return Result<IReadOnlyList<BlogPostDto>>.Success(fromRedis);
            }

            var posts = await repo.GetAllAsync(ct);
            var dtos = posts.Select(p => p.ToDto()).ToList();
            localCache.Set(CacheKey, dtos, LocalOpts);
            await distributedCache.SetAsync(CacheKey,
                JsonSerializer.SerializeToUtf8Bytes(dtos, JsonOpts), RedisOpts, ct);
            return Result<IReadOnlyList<BlogPostDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<BlogPostDto>>.Failure(ex.Message);
        }
    }
}
