//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     DeleteBlogPostHandler.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using MyBlog.Domain.Abstractions;
using MyBlog.Web.Infrastructure.Caching;

namespace MyBlog.Web.Features.BlogPosts.Delete;

public sealed class DeleteBlogPostHandler(
IBlogPostRepository repo,
IBlogPostCacheService cache) : IRequestHandler<DeleteBlogPostCommand, Result>
{
public async Task<Result> Handle(DeleteBlogPostCommand request, CancellationToken ct)
{
try
{
await repo.DeleteAsync(request.Id, ct);
await cache.InvalidateAllAsync(ct);
await cache.InvalidateByIdAsync(request.Id, ct);
return Result.Ok();
}
catch (DbUpdateConcurrencyException)
{
return Result.Fail(
"This post was modified by another user. Please reload and try again.",
ResultErrorCode.Concurrency);
}
catch (Exception ex)
{
return Result.Fail(ex.Message);
}
}
}
