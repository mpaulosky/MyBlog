//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     DomainLayerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Architecture.Tests
//=======================================================

namespace MyBlog.Architecture.Tests;

public class DomainLayerTests
{
	[Fact]
	public void Domain_Should_Not_Reference_Web()
	{
		// Arrange
		var assembly = typeof(BlogPost).Assembly;

		// Act
		var result = Types.InAssembly(assembly)
				.ShouldNot()
				.HaveDependencyOnAny("MyBlog.Web", "Microsoft.AspNetCore")
				.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue();
	}

	[Fact]
	public void Domain_Entities_Should_Be_Sealed()
	{
		// Arrange
		var assembly = typeof(BlogPost).Assembly;

		// Act
		var result = Types.InAssembly(assembly)
				.That()
				.ResideInNamespace("MyBlog.Domain.Entities")
				.Should()
				.BeSealed()
				.GetResult();

		// Assert
		result.IsSuccessful.Should().BeTrue();
	}

	[Fact]
	public void Domain_Should_Not_Have_InMemoryRepository()
	{
		// Arrange
		var assembly = typeof(BlogPost).Assembly;

		// Act
		var types = Types.InAssembly(assembly)
				.That()
				.HaveNameEndingWith("InMemory")
				.GetTypes();

		// Assert
		types.Should().BeEmpty("InMemoryBlogPostRepository should have been deleted");
	}

	[Fact]
	public void Domain_Should_Not_Have_Features()
	{
		// Arrange
		var assembly = typeof(BlogPost).Assembly;

		// Act
		var types = Types.InAssembly(assembly)
				.That()
				.ResideInNamespaceStartingWith("MyBlog.Domain.Features")
				.GetTypes();

		// Assert
		types.Should().BeEmpty("CQRS handlers and commands belong in the Web project (VSA)");
	}
}
