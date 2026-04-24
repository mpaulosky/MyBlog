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
public async Task<Result> Handle(DeleteBlogPostCommand request, CancellationToken cancellationToken)
{
try
{
await repo.DeleteAsync(request.Id, cancellationToken).ConfigureAwait(false);
await cache.InvalidateAllAsync(cancellationToken).ConfigureAwait(false);
await cache.InvalidateByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
return Result.Ok();
}
catch (DbUpdateConcurrencyException)
{
return Result.Fail(
"This post was modified by another user. Please reload and try again.",
ResultErrorCode.Concurrency);
}
catch (OperationCanceledException)
{
throw;
}
catch (InvalidOperationException ex)
{
return Result.Fail(ex.Message);
}
#pragma warning disable CA1031 // Intentional: top-level handler converts unexpected failures to Result to keep UI stable
catch (Exception)
{
return Result.Fail("An unexpected error occurred.");
}
#pragma warning restore CA1031
}
}
