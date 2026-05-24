//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     PassthroughBlogPostCacheService.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests.Integration
//=======================================================

namespace Web.Infrastructure;

internal sealed class PassthroughBlogPostCacheService : IBlogPostCacheService
{
	public ValueTask<IReadOnlyList<BlogPostDto>> GetOrFetchAllAsync(
		Func<Task<IReadOnlyList<BlogPostDto>>> fetch,
		CancellationToken ct = default) =>
		new(fetch());

	public ValueTask<BlogPostDto?> GetOrFetchByIdAsync(
		ObjectId id,
		Func<Task<BlogPostDto?>> fetch,
		CancellationToken ct = default) =>
		new(fetch());

	public Task InvalidateAllAsync(CancellationToken ct = default) => Task.CompletedTask;

	public Task InvalidateByIdAsync(ObjectId id, CancellationToken ct = default) => Task.CompletedTask;
}
