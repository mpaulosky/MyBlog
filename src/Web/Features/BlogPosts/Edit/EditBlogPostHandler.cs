//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     EditBlogPostHandler.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using MyBlog.Domain.Abstractions;
using MyBlog.Web.Infrastructure.Caching;

namespace MyBlog.Web.Features.BlogPosts.Edit;

public sealed class EditBlogPostHandler(
IBlogPostRepository repo,
IBlogPostCacheService cache)
: IRequestHandler<EditBlogPostCommand, Result>,
IRequestHandler<GetBlogPostByIdQuery, Result<BlogPostDto?>>
{
public async Task<Result> Handle(EditBlogPostCommand request, CancellationToken ct)
{
try
{
var post = await repo.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
if (post is null)
return Result.Fail($"BlogPost {request.Id} not found.");
post.Update(request.Title, request.Content);
await repo.UpdateAsync(post, ct).ConfigureAwait(false);
await cache.InvalidateAllAsync(ct).ConfigureAwait(false);
await cache.InvalidateByIdAsync(request.Id, ct).ConfigureAwait(false);
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
catch (Exception ex)
{
return Result.Fail(ex.Message);
}
}

public async Task<Result<BlogPostDto?>> Handle(GetBlogPostByIdQuery request, CancellationToken ct)
{
try
{
var dto = await cache.GetOrFetchByIdAsync(
request.Id,
async () =>
{
var post = await repo.GetByIdAsync(request.Id, ct).ConfigureAwait(false);
return post?.ToDto();
}, ct).ConfigureAwait(false);
return Result.Ok<BlogPostDto?>(dto);
}
catch (OperationCanceledException)
{
throw;
}
catch (Exception ex)
{
return Result.Fail<BlogPostDto?>(ex.Message);
}
}
}
