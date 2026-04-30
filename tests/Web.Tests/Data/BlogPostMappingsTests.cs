//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     BlogPostMappingsTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

namespace Unit.Data;

public class BlogPostMappingsTests
{
	[Fact]
	public void ToDto_MapsAllFields_Correctly()
	{
		// Arrange
		var post = BlogPost.Create("Test Title", "Test Content", "Test Author");

		// Act
		var dto = post.ToDto();

		// Assert
		dto.Id.Should().Be(post.Id);
		dto.Title.Should().Be(post.Title);
		dto.Content.Should().Be(post.Content);
		dto.Author.Should().Be(post.Author);
		dto.CreatedAt.Should().Be(post.CreatedAt);
	}

	[Fact]
	public void ToDto_UpdatedAt_IsNullOnNewPost()
	{
		// Arrange
		var post = BlogPost.Create("Title", "Content", "Author");

		// Act
		var dto = post.ToDto();

		// Assert
		dto.UpdatedAt.Should().BeNull();
	}

	[Fact]
	public void ToDto_UpdatedAt_IsSetAfterUpdate()
	{
		// Arrange
		var post = BlogPost.Create("Title", "Content", "Author");
		post.Update("New Title", "New Content");

		// Act
		var dto = post.ToDto();

		// Assert
		dto.UpdatedAt.Should().NotBeNull();
		dto.UpdatedAt.Should().Be(post.UpdatedAt);
	}

	[Fact]
	public void ToDto_IsPublished_FalseOnNewPost()
	{
		// Arrange
		var post = BlogPost.Create("Title", "Content", "Author");

		// Act
		var dto = post.ToDto();

		// Assert
		dto.IsPublished.Should().BeFalse();
	}

	[Fact]
	public void ToDto_IsPublished_TrueAfterPublish()
	{
		// Arrange
		var post = BlogPost.Create("Title", "Content", "Author");
		post.Publish();

		// Act
		var dto = post.ToDto();

		// Assert
		dto.IsPublished.Should().BeTrue();
	}

	[Fact]
	public void ToDto_IsPublished_FalseAfterUnpublish()
	{
		// Arrange
		var post = BlogPost.Create("Title", "Content", "Author");
		post.Publish();
		post.Unpublish();

		// Act
		var dto = post.ToDto();

		// Assert
		dto.IsPublished.Should().BeFalse();
	}
}
