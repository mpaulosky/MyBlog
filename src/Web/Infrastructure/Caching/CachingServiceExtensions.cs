//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CachingServiceExtensions.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

namespace MyBlog.Web.Infrastructure.Caching;

internal static class CachingServiceExtensions
{
	/// <summary>
	/// Registers the two-tier (L1 in-memory + L2 Redis) <see cref="IBlogPostCacheService"/>
	/// implementation. Call this after <c>AddMemoryCache()</c> and
	/// <c>AddRedisDistributedCache()</c> are already registered.
	/// </summary>
	public static IServiceCollection AddBlogPostCaching(this IServiceCollection services)
	{
		services.AddSingleton<IBlogPostCacheService, BlogPostCacheService>();
		return services;
	}

	/// <summary>
	/// Registers the two-tier (L1 in-memory 30s + L2 Redis 2min) <see cref="IUserManagementCacheService"/>
	/// implementation. Call this after <c>AddMemoryCache()</c> and
	/// <c>AddRedisDistributedCache()</c> are already registered.
	/// </summary>
	public static IServiceCollection AddUserManagementCaching(this IServiceCollection services)
	{
		services.AddSingleton<IUserManagementCacheService, UserManagementCacheService>();
		return services;
	}
}
