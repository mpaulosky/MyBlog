//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     MongoDbBlogPostCategoryTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests.Integration
//=======================================================

using Web.Infrastructure;

namespace Web.Categories;

/// <summary>
/// Integration tests for the Category-related methods on IBlogPostRepository:
/// ExistsByCategoryAsync — used by DeleteCategoryHandler to enforce the
/// "cannot delete category in use" acceptance criterion.
/// </summary>
[Collection("CategoryIntegration")]
public sealed class MongoDbBlogPostCategoryTests(MongoDbFixture fixture)
{
	private MongoDbBlogPostRepository CreateBlogPostRepo(string dbName) =>
		new(fixture.CreateFactory(dbName));

	private static readonly PostAuthor Author =
		new(string.Empty, "Test Author", string.Empty, []);

	[Fact]
	public async Task ExistsByCategoryAsync_ReturnsTrueWhenPostAssignedToCategory()
	{
		// Arrange — AC: cannot delete category in use
		var ct = TestContext.Current.CancellationToken;
		var dbName = $"T{Guid.NewGuid():N}";
		var repo = CreateBlogPostRepo(dbName);
		var categoryId = ObjectId.GenerateNewId();
		var post = BlogPost.Create("Hello World", "Content", Author);
		post.AssignCategory(categoryId);
		await repo.AddAsync(post, ct);

		// Act
		var exists = await repo.ExistsByCategoryAsync(categoryId, ct);

		// Assert
		exists.Should().BeTrue();
	}

	[Fact]
	public async Task ExistsByCategoryAsync_ReturnsFalseWhenNoCategoryAssigned()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		var dbName = $"T{Guid.NewGuid():N}";
		var repo = CreateBlogPostRepo(dbName);
		var categoryId = ObjectId.GenerateNewId();
		var post = BlogPost.Create("Hello World", "Content", Author);
		// No AssignCategory call — CategoryId is null
		await repo.AddAsync(post, ct);

		// Act
		var exists = await repo.ExistsByCategoryAsync(categoryId, ct);

		// Assert
		exists.Should().BeFalse();
	}

	[Fact]
	public async Task ExistsByCategoryAsync_ReturnsFalseWhenRepositoryEmpty()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		var repo = CreateBlogPostRepo($"T{Guid.NewGuid():N}");

		// Act
		var exists = await repo.ExistsByCategoryAsync(ObjectId.GenerateNewId(), ct);

		// Assert
		exists.Should().BeFalse();
	}

	[Fact]
	public async Task ExistsByCategoryAsync_ReturnsFalseAfterCategoryRemoved()
	{
		// Arrange — post had category, then category was removed
		var ct = TestContext.Current.CancellationToken;
		var dbName = $"T{Guid.NewGuid():N}";
		var repo = CreateBlogPostRepo(dbName);
		var categoryId = ObjectId.GenerateNewId();
		var post = BlogPost.Create("Post", "Content", Author);
		post.AssignCategory(categoryId);
		await repo.AddAsync(post, ct);
		post.RemoveCategory();
		await repo.UpdateAsync(post, ct);

		// Act
		var exists = await repo.ExistsByCategoryAsync(categoryId, ct);

		// Assert
		exists.Should().BeFalse();
	}
}
