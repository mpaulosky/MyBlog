//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     BlogPostCategoryTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain.Tests
//=======================================================

namespace Tests.Domain.Entities;

/// <summary>
/// Tests for BlogPost behavior related to Issue #339 — Category assignment,
/// author read-only immutability, and default (null) category on creation.
/// </summary>
public class BlogPostCategoryTests
{
	private static readonly PostAuthor TestAuthor = new("author-1", "Alice", "alice@example.com", ["Author"]);

	// ── CategoryId default ────────────────────────────────────────────────

	[Fact]
	public void Create_NewPost_CategoryIdIsNullByDefault()
	{
		// Arrange / Act
		var post = BlogPost.Create("My Title", "My Content", TestAuthor);

		// Assert
		post.CategoryId.Should().BeNull();
	}

	// ── AssignCategory ────────────────────────────────────────────────────

	[Fact]
	public void AssignCategory_SetsCategoryId()
	{
		// Arrange
		var post = BlogPost.Create("Title", "Content", TestAuthor);
		var categoryId = Guid.NewGuid();

		// Act
		post.AssignCategory(categoryId);

		// Assert
		post.CategoryId.Should().Be(categoryId);
	}

	[Fact]
	public void AssignCategory_DoesNotAlterTitleContentOrAuthor()
	{
		// Arrange
		var post = BlogPost.Create("Stable Title", "Stable Content", TestAuthor);
		var categoryId = Guid.NewGuid();

		// Act
		post.AssignCategory(categoryId);

		// Assert
		post.Title.Should().Be("Stable Title");
		post.Content.Should().Be("Stable Content");
		post.Author.Name.Should().Be("Alice");
	}

	[Fact]
	public void AssignCategory_CalledTwice_OverwritesWithLatestCategory()
	{
		// Arrange
		var post = BlogPost.Create("Title", "Content", TestAuthor);
		var firstCategoryId = Guid.NewGuid();
		var secondCategoryId = Guid.NewGuid();

		// Act
		post.AssignCategory(firstCategoryId);
		post.AssignCategory(secondCategoryId);

		// Assert
		post.CategoryId.Should().Be(secondCategoryId);
	}

	// ── RemoveCategory ────────────────────────────────────────────────────

	[Fact]
	public void RemoveCategory_SetsCategoryIdToNull()
	{
		// Arrange
		var post = BlogPost.Create("Title", "Content", TestAuthor);
		post.AssignCategory(Guid.NewGuid());

		// Act
		post.RemoveCategory();

		// Assert
		post.CategoryId.Should().BeNull();
	}

	// ── Author read-only contract (#339 AC: author read-only display) ─────

	[Fact]
	public void Update_DoesNotModifyAuthor_AuthorRemainsImmutableAfterUpdate()
	{
		// Arrange
		var post = BlogPost.Create("Original Title", "Original Content", TestAuthor);

		// Act
		post.Update("New Title", "New Content");

		// Assert
		post.Author.Id.Should().Be("author-1");
		post.Author.Name.Should().Be("Alice");
		post.Author.Email.Should().Be("alice@example.com");
	}

	[Fact]
	public void Update_AuthorIsSnapshotFromCreationTime_CannotBeChangedByUpdate()
	{
		// Arrange — two different authors
		var originalAuthor = new PostAuthor("id-original", "Original Author", "orig@example.com", []);
		var post = BlogPost.Create("Title", "Content", originalAuthor);

		// Act — Update only accepts title and content; there is no author parameter
		post.Update("New Title", "New Content");

		// Assert — author is still the original snapshot
		post.Author.Name.Should().Be("Original Author");
		post.Author.Should().Be(originalAuthor);
	}
}
