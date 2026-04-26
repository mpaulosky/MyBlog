//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CachingLayerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Architecture.Tests
//=======================================================

using MyBlog.Web.Features.BlogPosts.List;

namespace MyBlog.Architecture.Tests;

public class CachingLayerTests
{
	private static readonly System.Reflection.Assembly WebAssembly = typeof(GetBlogPostsQuery).Assembly;

	[Fact]
	public void Features_Should_Not_Reference_IDistributedCache_Directly()
	{
		// Arrange / Act
		var result = Types.InAssembly(WebAssembly)
				.That()
				.ResideInNamespace("MyBlog.Web.Features")
				.ShouldNot()
				.HaveDependencyOnAny("Microsoft.Extensions.Caching.Distributed")
				.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			"VSA handlers must delegate caching to IBlogPostCacheService, not reference IDistributedCache directly");
	}

	[Fact]
	public void Features_Should_Not_Reference_IMemoryCache_Directly()
	{
		// Arrange / Act
		var result = Types.InAssembly(WebAssembly)
				.That()
				.ResideInNamespace("MyBlog.Web.Features")
				.ShouldNot()
				.HaveDependencyOnAny("Microsoft.Extensions.Caching.Memory")
				.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue(
			"VSA handlers must delegate caching to IBlogPostCacheService, not reference IMemoryCache directly");
	}
}
