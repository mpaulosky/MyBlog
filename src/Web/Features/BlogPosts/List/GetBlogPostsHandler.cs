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
var all = await repo.GetAllAsync(cancellationToken).ConfigureAwait(false);
return (IReadOnlyList<BlogPostDto>)all.Select(p => p.ToDto()).ToList();
}, cancellationToken).ConfigureAwait(false);
return Result.Ok<IReadOnlyList<BlogPostDto>>(result);
}
catch (OperationCanceledException)
{
throw;
}
catch (InvalidOperationException ex)
{
return Result.Fail<IReadOnlyList<BlogPostDto>>(ex.Message);
}
#pragma warning disable CA1031 // Intentional: top-level handler converts unexpected failures to Result to keep UI stable
catch (Exception)
{
return Result.Fail<IReadOnlyList<BlogPostDto>>("An unexpected error occurred.");
}
#pragma warning restore CA1031
}
}
