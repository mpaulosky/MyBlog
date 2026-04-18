namespace MyBlog.Architecture.Tests;

public class DomainLayerTests
{
	[Fact]
	public void Domain_Should_Not_Reference_Web()
	{
		var result = Types.InAssembly(typeof(BlogPost).Assembly)
				.ShouldNot()
				.HaveDependencyOnAny("MyBlog.Web", "Microsoft.AspNetCore")
				.GetResult();

		result.IsSuccessful.Should().BeTrue();
	}

	[Fact]
	public void Domain_Entities_Should_Be_Sealed()
	{
		var result = Types.InAssembly(typeof(BlogPost).Assembly)
				.That()
				.ResideInNamespace("MyBlog.Domain.Entities")
				.Should()
				.BeSealed()
				.GetResult();

		result.IsSuccessful.Should().BeTrue();
	}

	[Fact]
	public void Domain_Should_Not_Have_InMemoryRepository()
	{
		var types = Types.InAssembly(typeof(BlogPost).Assembly)
				.That()
				.HaveNameEndingWith("InMemory")
				.GetTypes();

		types.Should().BeEmpty("InMemoryBlogPostRepository should have been deleted");
	}
}
