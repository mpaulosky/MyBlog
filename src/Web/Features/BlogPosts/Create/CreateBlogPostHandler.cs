using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using MyBlog.Domain.Entities;
using MyBlog.Domain.Interfaces;
using MyBlog.Domain.Common;

namespace MyBlog.Web.Features.BlogPosts.Create;

public sealed class CreateBlogPostHandler(
    IBlogPostRepository repo,
    IMemoryCache localCache,
    IDistributedCache distributedCache) : IRequestHandler<CreateBlogPostCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateBlogPostCommand request, CancellationToken ct)
    {
        try
        {
            var post = BlogPost.Create(request.Title, request.Content, request.Author);
            await repo.AddAsync(post, ct);
            localCache.Remove("blog:all");
            _ = distributedCache.RemoveAsync("blog:all", ct);
            return Result<Guid>.Success(post.Id);
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
    }
}
