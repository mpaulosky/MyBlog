//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     ThemeLayerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Architecture.Tests
//=======================================================

using MyBlog.Web.Features.BlogPosts.List;

namespace MyBlog.Architecture.Tests;

public class ThemeLayerTests
{
	private static readonly System.Reflection.Assembly WebAssembly = typeof(GetBlogPostsQuery).Assembly;

	[Fact]
	public void ThemeComponents_ShouldResideIn_ThemeNamespace()
	{
		// Arrange / Act
		var result = Types.InAssembly(WebAssembly)
				.That()
				.ResideInNamespace("MyBlog.Web.Components.Theme")
				.Should()
				.ResideInNamespace("MyBlog.Web.Components.Theme")
				.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue();
	}

	[Fact]
	public void ThemeComponents_ShouldHaveNoDependencyOn_DomainOrMongoDB()
	{
		// Arrange / Act
		var result = Types.InAssembly(WebAssembly)
				.That()
				.ResideInNamespace("MyBlog.Web.Components.Theme")
				.ShouldNot()
				.HaveDependencyOnAny("MyBlog.Domain", "MongoDB")
				.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue();
	}
}
