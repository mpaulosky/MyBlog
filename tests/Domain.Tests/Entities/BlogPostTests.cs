//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     BlogPostTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain.Tests
//=======================================================

namespace MyBlog.Domain.Tests.Entities;

public class BlogPostTests
{
	[Fact]
	public void Create_ValidArguments_ReturnsEntityWithCorrectFields()
	{
		// Arrange
		const string title = "Test Title";
		const string content = "Test Content";
		const string author = "Test Author";

		// Act
		var post = BlogPost.Create(title, content, author);

		// Assert
		post.Title.Should().Be(title);
		post.Content.Should().Be(content);
		post.Author.Should().Be(author);
	}

	[Fact]
	public void Create_ValidArguments_IdIsNonEmptyGuid()
	{
		// Arrange / Act
		var post = BlogPost.Create("Title", "Content", "Author");

		// Assert
		post.Id.Should().NotBeEmpty();
	}

	[Fact]
	public void Create_ValidArguments_CreatedAtIsSet()
	{
		// Arrange
		var before = DateTime.UtcNow;

		// Act
		var post = BlogPost.Create("Title", "Content", "Author");

		// Assert
		post.CreatedAt.Should().BeOnOrAfter(before);
		post.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
	}

	[Fact]
	public void Create_ValidArguments_UpdatedAtIsNull()
	{
		// Arrange / Act
		var post = BlogPost.Create("Title", "Content", "Author");

		// Assert
		post.UpdatedAt.Should().BeNull();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Create_NullOrWhiteSpaceTitle_ThrowsArgumentException(string? title)
	{
		// Arrange / Act
		var act = () => BlogPost.Create(title!, "Content", "Author");

		// Assert
		act.Should().Throw<ArgumentException>();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Create_NullOrWhiteSpaceContent_ThrowsArgumentException(string? content)
	{
		// Arrange / Act
		var act = () => BlogPost.Create("Title", content!, "Author");

		// Assert
		act.Should().Throw<ArgumentException>();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Create_NullOrWhiteSpaceAuthor_ThrowsArgumentException(string? author)
	{
		// Arrange / Act
		var act = () => BlogPost.Create("Title", "Content", author!);

		// Assert
		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void Update_ValidArguments_UpdatesFieldsAndSetsUpdatedAt()
	{
		// Arrange
		var post = BlogPost.Create("Original Title", "Original Content", "Author");
		var before = DateTime.UtcNow;

		// Act
		post.Update("New Title", "New Content");

		// Assert
		post.Title.Should().Be("New Title");
		post.Content.Should().Be("New Content");
		post.UpdatedAt.Should().NotBeNull();
		post.UpdatedAt!.Value.Should().BeOnOrAfter(before);
	}

	[Fact]
	public void Update_ValidArguments_UpdatedAtIsNullBeforeUpdate_NonNullAfter()
	{
		// Arrange
		var post = BlogPost.Create("Title", "Content", "Author");
		post.UpdatedAt.Should().BeNull();

		// Act
		post.Update("New Title", "New Content");

		// Assert
		post.UpdatedAt.Should().NotBeNull();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Update_NullOrWhiteSpaceTitle_ThrowsArgumentException(string? title)
	{
		// Arrange
		var post = BlogPost.Create("Title", "Content", "Author");

		// Act
		var act = () => post.Update(title!, "New Content");

		// Assert
		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void Publish_SetsIsPublishedTrue()
	{
		// Arrange
		var post = BlogPost.Create("Title", "Content", "Author");

		// Act
		post.Publish();

		// Assert
		post.IsPublished.Should().BeTrue();
	}

	[Fact]
	public void Unpublish_SetsIsPublishedFalse()
	{
		// Arrange
		var post = BlogPost.Create("Title", "Content", "Author");
		post.Publish();

		// Act
		post.Unpublish();

		// Assert
		post.IsPublished.Should().BeFalse();
	}

	[Fact]
	public void Create_NewPost_IsPublishedIsFalse()
	{
		// Arrange / Act
		var post = BlogPost.Create("Title", "Content", "Author");

		// Assert
		post.IsPublished.Should().BeFalse();
	}
}
