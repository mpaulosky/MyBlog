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

internal sealed class EditBlogPostHandler(
IBlogPostRepository repo,
IBlogPostCacheService cache)
: IRequestHandler<EditBlogPostCommand, Result>,
IRequestHandler<GetBlogPostByIdQuery, Result<BlogPostDto?>>
{
	public async Task<Result> Handle(EditBlogPostCommand request, CancellationToken cancellationToken)
	{
		try
		{
			var post = await repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
			if (post is null)
				return Result.Fail($"BlogPost {request.Id} not found.");
			post.Update(request.Title, request.Content);
			await repo.UpdateAsync(post, cancellationToken).ConfigureAwait(false);
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

	public async Task<Result<BlogPostDto?>> Handle(GetBlogPostByIdQuery request, CancellationToken cancellationToken)
	{
		try
		{
			var dto = await cache.GetOrFetchByIdAsync(
			request.Id,
			async () =>
			{
				var post = await repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
				return post?.ToDto();
			}, cancellationToken).ConfigureAwait(false);
			return Result.Ok<BlogPostDto?>(dto);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (InvalidOperationException ex)
		{
			return Result.Fail<BlogPostDto?>(ex.Message);
		}
#pragma warning disable CA1031 // Intentional: top-level handler converts unexpected failures to Result to keep UI stable
		catch (Exception)
		{
			return Result.Fail<BlogPostDto?>("An unexpected error occurred.");
		}
#pragma warning restore CA1031
	}
}
