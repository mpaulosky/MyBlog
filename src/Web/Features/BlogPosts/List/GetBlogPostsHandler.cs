//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     GetBlogPostsHandler.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using MyBlog.Domain.Abstractions;
using MyBlog.Web.Infrastructure.Caching;

namespace MyBlog.Web.Features.BlogPosts.List;

public sealed class GetBlogPostsHandler(
IBlogPostRepository repo,
IBlogPostCacheService cache) : IRequestHandler<GetBlogPostsQuery, Result<IReadOnlyList<BlogPostDto>>>
{
public async Task<Result<IReadOnlyList<BlogPostDto>>> Handle(
GetBlogPostsQuery request, CancellationToken cancellationToken)
{
try
{
var result = await cache.GetOrFetchAllAsync(
async () =>
{
var all = await repo.GetAllAsync(cancellationToken);
return (IReadOnlyList<BlogPostDto>)all.Select(p => p.ToDto()).ToList();
}, cancellationToken);
return Result.Ok<IReadOnlyList<BlogPostDto>>(result);
}
catch (Exception ex)
{
return Result.Fail<IReadOnlyList<BlogPostDto>>(ex.Message);
}
}
}
