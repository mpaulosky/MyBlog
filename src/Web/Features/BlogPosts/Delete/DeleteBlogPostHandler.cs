//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     DeleteBlogPostHandler.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using MyBlog.Domain.Abstractions;

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
