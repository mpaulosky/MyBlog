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
	private static readonly PostAuthor TestAuthor = new("test-id", "Test Author", "test@example.com", []);

	[Fact]
	public void Create_WithValidArgs_ReturnsBlogPost()
	{
		// Arrange (none)

		// Act
		var post = BlogPost.Create("Test Title", "Test Content", TestAuthor);

		// Assert
		post.Id.Should().NotBeEmpty();
		post.Title.Should().Be("Test Title");
		post.Content.Should().Be("Test Content");
		post.Author.Name.Should().Be("Test Author");
		post.IsPublished.Should().BeFalse();
		post.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
	}

	[Theory]
	[InlineData("", "content")]
	[InlineData("title", "")]
	public void Create_WithBlankTitleOrContent_ThrowsArgumentException(string title, string content)
	{
		// Arrange
		var act = () => BlogPost.Create(title, content, TestAuthor);

		// Act & Assert
		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void Create_WithNullAuthor_ThrowsArgumentNullException()
	{
		// Arrange
		var act = () => BlogPost.Create("title", "content", null!);

		// Act & Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Fact]
	public void Create_WithEmptyAuthorName_ThrowsArgumentException()
	{
		// Arrange
		var act = () => BlogPost.Create("title", "content", new PostAuthor("", "", "", []));

		// Act & Assert
		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void Update_ChangesTitle_AndContent()
	{
		// Arrange
		var post = BlogPost.Create("Old Title", "Old Content", TestAuthor);

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
		var post = BlogPost.Create("T", "C", TestAuthor);

		// Act
		post.Publish();

		// Assert
		post.IsPublished.Should().BeTrue();
	}

	[Fact]
	public void Unpublish_SetsIsPublished_False()
	{
		// Arrange
		var post = BlogPost.Create("T", "C", TestAuthor);
		post.Publish();

		// Act
		post.Unpublish();

		// Assert
		post.IsPublished.Should().BeFalse();
	}
}
