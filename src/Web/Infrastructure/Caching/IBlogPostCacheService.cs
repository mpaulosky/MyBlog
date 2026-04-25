//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     IBlogPostCacheService.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using MyBlog.Web.Data;

namespace MyBlog.Web.Infrastructure.Caching;

/// <summary>
/// Two-tier (L1 in-memory + L2 Redis) cache abstraction for <see cref="BlogPostDto"/> values.
/// </summary>
/// <remarks>
/// Registered as a singleton. Both <see cref="IMemoryCache"/> and
/// <see cref="IDistributedCache"/> (StackExchange Redis) are also singletons,
/// so captive-dependency rules are satisfied.
/// </remarks>
internal interface IBlogPostCacheService
{
	/// <summary>
	/// Returns all blog posts from the nearest cache tier, or invokes
	/// <paramref name="fetch"/> on a complete miss, populates both tiers, and returns the result.
	/// </summary>
	/// <remarks>
	/// Returns <see cref="ValueTask{T}"/> because L1 hits complete synchronously.
	/// Do not await the same <see cref="ValueTask{T}"/> instance more than once.
	/// </remarks>
	ValueTask<IReadOnlyList<BlogPostDto>> GetOrFetchAllAsync(
		Func<Task<IReadOnlyList<BlogPostDto>>> fetch,
		CancellationToken ct = default);

	/// <summary>
	/// Returns the blog post with <paramref name="id"/> from the nearest cache tier,
	/// or invokes <paramref name="fetch"/> on a complete miss.
	/// Returns <c>null</c> when the post does not exist.
	/// </summary>
	/// <remarks>
	/// Returns <see cref="ValueTask{T}"/> because L1 hits complete synchronously.
	/// Do not await the same <see cref="ValueTask{T}"/> instance more than once.
	/// </remarks>
	ValueTask<BlogPostDto?> GetOrFetchByIdAsync(
		Guid id,
		Func<Task<BlogPostDto?>> fetch,
		CancellationToken ct = default);

	/// <summary>
	/// Removes the "all posts" entry from both cache tiers.
	/// </summary>
	/// <remarks>
	/// Redis removal uses <see cref="CancellationToken.None"/>: the database write has
	/// already committed and invalidation must complete regardless of caller cancellation.
	/// </remarks>
	Task InvalidateAllAsync(CancellationToken ct = default);

	/// <summary>
	/// Removes the per-post entry for <paramref name="id"/> from both cache tiers.
	/// </summary>
	/// <remarks>
	/// Redis removal uses <see cref="CancellationToken.None"/>: the database write has
	/// already committed and invalidation must complete regardless of caller cancellation.
	/// </remarks>
	Task InvalidateByIdAsync(Guid id, CancellationToken ct = default);
}
