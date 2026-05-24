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
	private static readonly PostAuthor TestAuthor = new("test-id", "Test Author", "test@example.com", ["Author"]);

	[Fact]
	public void ToDto_MapsAllFields_Correctly()
	{
		// Arrange
		var post = BlogPost.Create("Test Title", "Test Content", TestAuthor);

		// Act
		var dto = post.ToDto();

		// Assert
		dto.Id.Should().Be(post.Id.ToString());
		dto.Title.Should().Be(post.Title);
		dto.Content.Should().Be(post.Content);
		dto.AuthorId.Should().Be(post.Author.Id);
		dto.AuthorName.Should().Be(post.Author.Name);
		dto.AuthorEmail.Should().Be(post.Author.Email);
		dto.AuthorRoles.Should().BeEquivalentTo(post.Author.Roles);
		dto.CreatedAt.Should().Be(post.CreatedAt);
	}

	[Fact]
	public void ToDto_UpdatedAt_IsNullOnNewPost()
	{
		// Arrange
		var post = BlogPost.Create("Title", "Content", new PostAuthor("", "Test Author", "", []));

		// Act
		var dto = post.ToDto();

		// Assert
		dto.UpdatedAt.Should().BeNull();
	}

	[Fact]
	public void ToDto_UpdatedAt_IsSetAfterUpdate()
	{
		// Arrange
		var post = BlogPost.Create("Title", "Content", new PostAuthor("", "Test Author", "", []));
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
		var post = BlogPost.Create("Title", "Content", new PostAuthor("", "Test Author", "", []));

		// Act
		var dto = post.ToDto();

		// Assert
		dto.IsPublished.Should().BeFalse();
	}

	[Fact]
	public void ToDto_IsPublished_TrueAfterPublish()
	{
		// Arrange
		var post = BlogPost.Create("Title", "Content", new PostAuthor("", "Test Author", "", []));
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
		var post = BlogPost.Create("Title", "Content", new PostAuthor("", "Test Author", "", []));
		post.Publish();
		post.Unpublish();

		// Act
		var dto = post.ToDto();

		// Assert
		dto.IsPublished.Should().BeFalse();
	}
}
