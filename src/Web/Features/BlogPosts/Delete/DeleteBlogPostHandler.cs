using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using MyBlog.Domain.Interfaces;
using MyBlog.Domain.Common;

namespace MyBlog.Web.Features.BlogPosts.Delete;

public sealed class DeleteBlogPostHandler(
    IBlogPostRepository repo,
    IMemoryCache localCache,
    IDistributedCache distributedCache) : IRequestHandler<DeleteBlogPostCommand, Result>
{
    public async Task<Result> Handle(DeleteBlogPostCommand request, CancellationToken ct)
    {
        try
        {
            await repo.DeleteAsync(request.Id, ct);
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
}
