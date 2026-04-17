using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using MyBlog.Domain.Interfaces;
using MyBlog.Web.Data;
using MyBlog.Domain.Common;

namespace MyBlog.Web.Features.BlogPosts.Edit;

public sealed class EditBlogPostHandler(
    IBlogPostRepository repo,
    IMemoryCache localCache,
    IDistributedCache distributedCache)
    : IRequestHandler<EditBlogPostCommand, Result>,
      IRequestHandler<GetBlogPostByIdQuery, Result<BlogPostDto?>>
{
    private static readonly MemoryCacheEntryOptions LocalOpts =
        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(1));
    private static readonly DistributedCacheEntryOptions RedisOpts =
        new DistributedCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public async Task<Result> Handle(EditBlogPostCommand request, CancellationToken ct)
    {
        try
        {
            var post = await repo.GetByIdAsync(request.Id, ct);
            if (post is null)
                return Result.Failure($"BlogPost {request.Id} not found.");
            post.Update(request.Title, request.Content);
            await repo.UpdateAsync(post, ct);
            localCache.Remove("blog:all");
            localCache.Remove($"blog:{request.Id}");
            await distributedCache.RemoveAsync("blog:all", ct);
            await distributedCache.RemoveAsync($"blog:{request.Id}", ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result<BlogPostDto?>> Handle(GetBlogPostByIdQuery request, CancellationToken ct)
    {
        try
        {
            var key = $"blog:{request.Id}";
            if (localCache.TryGetValue(key, out BlogPostDto? cached) && cached is not null)
                return Result<BlogPostDto?>.Success(cached);

            var bytes = await distributedCache.GetAsync(key, ct);
            if (bytes is not null)
            {
                var dto = JsonSerializer.Deserialize<BlogPostDto>(bytes, JsonOpts)!;
                localCache.Set(key, dto, LocalOpts);
                return Result<BlogPostDto?>.Success(dto);
            }

            var post = await repo.GetByIdAsync(request.Id, ct);
            if (post is null) return Result<BlogPostDto?>.Success(null);
            var result = post.ToDto();
            localCache.Set(key, result, LocalOpts);
            await distributedCache.SetAsync(key,
                JsonSerializer.SerializeToUtf8Bytes(result, JsonOpts), RedisOpts, ct);
            return Result<BlogPostDto?>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<BlogPostDto?>.Failure(ex.Message);
        }
    }
}
