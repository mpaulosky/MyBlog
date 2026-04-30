//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     BlogPostTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

namespace Web;

public class BlogPostTests
{
	[Fact]
	public void Create_WithValidArgs_ReturnsBlogPost()
	{
		// Arrange (none)

		// Act
		var post = BlogPost.Create("Test Title", "Test Content", "Test Author");

		// Assert
		post.Id.Should().NotBeEmpty();
		post.Title.Should().Be("Test Title");
		post.Content.Should().Be("Test Content");
		post.Author.Should().Be("Test Author");
		post.IsPublished.Should().BeFalse();
		post.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
	}

	[Theory]
	[InlineData("", "content", "author")]
	[InlineData("title", "", "author")]
	[InlineData("title", "content", "")]
	public void Create_WithBlankArgs_ThrowsArgumentException(string title, string content, string author)
	{
		// Arrange
		var act = () => BlogPost.Create(title, content, author);

		// Act & Assert
		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void Update_ChangesTitle_AndContent()
	{
		// Arrange
		var post = BlogPost.Create("Old Title", "Old Content", "Author");

		// Act
		post.Update("New Title", "New Content");

		// Assert
		post.Title.Should().Be("New Title");
		post.Content.Should().Be("New Content");
		post.UpdatedAt.Should().NotBeNull();
	}

	[Fact]
	public void Publish_SetsIsPublished_True()
	{
		// Arrange
		var post = BlogPost.Create("T", "C", "A");

		// Act
		post.Publish();

		// Assert
		post.IsPublished.Should().BeTrue();
	}

	[Fact]
	public void Unpublish_SetsIsPublished_False()
	{
		// Arrange
		var post = BlogPost.Create("T", "C", "A");
		post.Publish();

		// Act
		post.Unpublish();

		// Assert
		post.IsPublished.Should().BeFalse();
	}
}
