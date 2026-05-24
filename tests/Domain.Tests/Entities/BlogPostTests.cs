//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     BlogPostTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain.Tests
//=======================================================

namespace Tests.Domain.Entities;

public class BlogPostTests
{
	private static readonly PostAuthor TestAuthor = new("test-id", "Test Author", "test@example.com", []);

	[Fact]
	public void Create_ValidArguments_ReturnsEntityWithCorrectFields()
	{
		// Arrange / Act
		var post = BlogPost.Create("Test Title", "Test Content", TestAuthor);

		// Assert
		post.Title.Should().Be("Test Title");
		post.Content.Should().Be("Test Content");
		post.Author.Name.Should().Be("Test Author");
	}

	[Fact]
	public void Create_ValidArguments_IdIsNonEmptyGuid()
	{
		// Arrange / Act
		var post = BlogPost.Create("Title", "Content", TestAuthor);

		// Assert
		post.Id.Should().NotBe(ObjectId.Empty);
	}

	[Fact]
	public void Create_ValidArguments_CreatedAtIsSet()
	{
		// Arrange
		var before = DateTime.UtcNow;

		// Act
		var post = BlogPost.Create("Title", "Content", TestAuthor);

		// Assert
		post.CreatedAt.Should().BeOnOrAfter(before);
		post.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
	}

	[Fact]
	public void Create_ValidArguments_UpdatedAtIsNull()
	{
		// Arrange / Act
		var post = BlogPost.Create("Title", "Content", TestAuthor);

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
		var act = () => BlogPost.Create(title!, "Content", TestAuthor);

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
		var act = () => BlogPost.Create("Title", content!, TestAuthor);

		// Assert
		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void Create_NullAuthor_ThrowsArgumentNullException()
	{
		// Arrange / Act
		var act = () => BlogPost.Create("Title", "Content", null!);

		// Assert
		act.Should().Throw<ArgumentNullException>();
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void Create_WhiteSpaceAuthorName_ThrowsArgumentException(string? authorName)
	{
		// Arrange / Act
		var act = () => BlogPost.Create("Title", "Content", new PostAuthor("", authorName!, "", []));

		// Assert
		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void Update_ValidArguments_UpdatesFieldsAndSetsUpdatedAt()
	{
		// Arrange
		var post = BlogPost.Create("Original Title", "Original Content", TestAuthor);
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
		var post = BlogPost.Create("Title", "Content", TestAuthor);
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
		var post = BlogPost.Create("Title", "Content", TestAuthor);

		// Act
		var act = () => post.Update(title!, "New Content");

		// Assert
		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void Publish_SetsIsPublishedTrue()
	{
		// Arrange
		var post = BlogPost.Create("Title", "Content", TestAuthor);

		// Act
		post.Publish();

		// Assert
		post.IsPublished.Should().BeTrue();
	}

	[Fact]
	public void Unpublish_SetsIsPublishedFalse()
	{
		// Arrange
		var post = BlogPost.Create("Title", "Content", TestAuthor);
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
		var post = BlogPost.Create("Title", "Content", TestAuthor);

		// Assert
		post.IsPublished.Should().BeFalse();
	}
}
