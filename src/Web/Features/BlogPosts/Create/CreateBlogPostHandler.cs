//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CreateBlogPostHandler.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using Ganss.Xss;

using Microsoft.Extensions.Logging;

using MyBlog.Domain.Abstractions;
using MyBlog.Web.Infrastructure.Caching;

namespace MyBlog.Web.Features.BlogPosts.Create;

internal sealed partial class CreateBlogPostHandler(
IBlogPostRepository repo,
IBlogPostCacheService cache,
IHtmlSanitizer sanitizer,
ILogger<CreateBlogPostHandler> logger) : IRequestHandler<CreateBlogPostCommand, Result<ObjectId>>
{
	[LoggerMessage(Level = LogLevel.Warning, Message = "HTML sanitized on CreateBlogPost — unsafe markup was removed. Title: {Title}")]
	private static partial void LogHtmlSanitized(ILogger logger, string title);

	public async Task<Result<ObjectId>> Handle(CreateBlogPostCommand request, CancellationToken cancellationToken)
	{
		try
		{
			var sanitizedContent = sanitizer.Sanitize(request.Content);
			if (sanitizedContent != request.Content)
			{
				LogHtmlSanitized(logger, request.Title);
			}

			if (string.IsNullOrWhiteSpace(sanitizedContent))
			{
				return Result.Fail<ObjectId>("Content is empty after sanitization. Please provide valid content.");
			}

			var post = BlogPost.Create(request.Title, sanitizedContent, request.Author);
			if (request.IsPublished)
			{
				post.Publish();
			}

			if (request.CategoryId is not null)
			{
				post.AssignCategory(request.CategoryId.Value);
			}

			await repo.AddAsync(post, cancellationToken).ConfigureAwait(false);
			await cache.InvalidateAllAsync(cancellationToken).ConfigureAwait(false);
			return Result.Ok<ObjectId>(post.Id);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (InvalidOperationException ex)
		{
			return Result.Fail<ObjectId>(ex.Message);
		}
#pragma warning disable CA1031 // Intentional: top-level handler converts unexpected failures to Result to keep UI stable
		catch (Exception)
		{
			return Result.Fail<ObjectId>("An unexpected error occurred.");
		}
#pragma warning restore CA1031
	}
}
