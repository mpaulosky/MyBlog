//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     EditBlogPostHandler.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using Ganss.Xss;

using Microsoft.Extensions.Logging;

using MyBlog.Domain.Abstractions;
using MyBlog.Web.Infrastructure.Caching;

namespace MyBlog.Web.Features.BlogPosts.Edit;

internal sealed partial class EditBlogPostHandler(
IBlogPostRepository repo,
IBlogPostCacheService cache,
IHtmlSanitizer sanitizer,
ILogger<EditBlogPostHandler> logger)
: IRequestHandler<EditBlogPostCommand, Result>,
IRequestHandler<GetBlogPostByIdQuery, Result<BlogPostDto?>>
{
	[LoggerMessage(Level = LogLevel.Warning, Message = "HTML sanitized on EditBlogPost — unsafe markup was removed. PostId: {PostId}")]
	private static partial void LogHtmlSanitized(ILogger logger, ObjectId postId);

	public async Task<Result> Handle(EditBlogPostCommand request, CancellationToken cancellationToken)
	{
		try
		{
			var sanitizedContent = sanitizer.Sanitize(request.Content);
			if (sanitizedContent != request.Content)
			{
				LogHtmlSanitized(logger, request.Id);
			}

			if (string.IsNullOrWhiteSpace(sanitizedContent))
			{
				return Result.Fail("Content is empty after sanitization. Please provide valid content.");
			}

			var post = await repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
			if (post is null)
				return Result.Fail($"BlogPost {request.Id} not found.");

			if (!request.CallerIsAdmin && post.Author.Id != request.CallerUserId)
				return Result.Fail("You are not authorized to edit this post.", ResultErrorCode.Unauthorized);

			// Pass categoryId into Update() so all field mutations happen inside a single Version++.
			// Calling AssignCategory() separately after Update() would double-increment Version,
			// making UpdateAsync's OriginalValue = Version - 1 point to the wrong generation
			// and causing a false DbUpdateConcurrencyException on every category-bearing edit.
			post.Update(request.Title, sanitizedContent, request.CategoryId);
			if (request.IsPublished is true)
			{
				post.Publish();
			}
			else if (request.IsPublished is false)
			{
				post.Unpublish();
			}

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
