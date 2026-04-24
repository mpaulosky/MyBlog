//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     BlogPostCacheKeys.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

namespace MyBlog.Web.Infrastructure.Caching;

/// <summary>Cache key constants for the BlogPost two-tier cache.</summary>
internal static class BlogPostCacheKeys
{
	/// <summary>Key for the list of all blog posts.</summary>
	public const string All = "blog:all";

	/// <summary>Key for a single blog post identified by <paramref name="id"/>.</summary>
	public static string ById(Guid id) => $"blog:{id}";
}
