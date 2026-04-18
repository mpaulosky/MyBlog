using MyBlog.Web.Features.BlogPosts.List;

namespace MyBlog.Architecture.Tests;

public class VsaLayerTests
{
	private static readonly System.Reflection.Assembly WebAssembly = typeof(GetBlogPostsQuery).Assembly;

	[Fact]
	public void Features_Should_Not_Reference_Each_Other()
	{
		// BlogPosts slice should not reference UserManagement and vice versa
		var blogPostsResult = Types.InAssembly(WebAssembly)
				.That()
				.ResideInNamespace("MyBlog.Web.Features.BlogPosts")
				.ShouldNot()
				.HaveDependencyOnAny("MyBlog.Web.Features.UserManagement")
				.GetResult();

		blogPostsResult.IsSuccessful.Should().BeTrue();
	}

	[Fact]
	public void Handlers_Should_HaveNameEndingWithHandler_And_BeSealed()
	{
		// All types named *Handler in Web assembly should be sealed classes
		var result = Types.InAssembly(WebAssembly)
				.That()
				.HaveNameEndingWith("Handler")
				.Should()
				.BeSealed()
				.GetResult();

		result.IsSuccessful.Should().BeTrue();
	}

	[Fact]
	public void Data_Layer_Should_Not_Be_Referenced_Outside_Web()
	{
		// Domain assembly must NOT depend on MyBlog.Web.Data
		var result = Types.InAssembly(typeof(BlogPost).Assembly)
				.ShouldNot()
				.HaveDependencyOnAny("MyBlog.Web.Data")
				.GetResult();

		result.IsSuccessful.Should().BeTrue();
	}
}
