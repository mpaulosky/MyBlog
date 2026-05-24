//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CategoryObjectIdWorkflowTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests.Integration
//=======================================================

using Ganss.Xss;

using Microsoft.Extensions.Logging.Abstractions;

using MyBlog.Domain.Abstractions;
using MyBlog.Domain.ValueObjects;
using MyBlog.Web.Features.BlogPosts.Create;
using MyBlog.Web.Features.Categories.Create;
using MyBlog.Web.Features.Categories.Delete;
using MyBlog.Web.Features.Categories.GetById;

using Web.Infrastructure;

namespace Web.Categories;

[Collection("CategoryIntegration")]
public sealed class CategoryObjectIdWorkflowTests(MongoDbFixture fixture)
{
	private static readonly PostAuthor Author = new("auth0|author-1", "Author One", "author@example.com", ["Author"]);

	[Fact]
	public async Task CategoryHandlers_Block_Delete_When_Real_BlogPost_References_Category_ObjectId()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		var dbName = $"T{Guid.NewGuid():N}";
		var categoryRepo = new MongoDbCategoryRepository(fixture.CreateFactory(dbName));
		var blogPostRepo = new MongoDbBlogPostRepository(fixture.CreateFactory(dbName));
		var createCategoryHandler = new CreateCategoryHandler(categoryRepo);
		var getCategoryByIdHandler = new GetCategoryByIdHandler(categoryRepo);
		var createBlogPostHandler = new CreateBlogPostHandler(
			blogPostRepo,
			new PassthroughBlogPostCacheService(),
			new HtmlSanitizer(),
			NullLogger<CreateBlogPostHandler>.Instance);
		var deleteCategoryHandler = new DeleteCategoryHandler(categoryRepo, blogPostRepo);

		// Act
		var createdCategory = await createCategoryHandler.Handle(
			new CreateCategoryCommand("Technology", "Tech posts."),
			ct);
		var loadedCategory = await getCategoryByIdHandler.Handle(
			new GetCategoryByIdQuery(createdCategory.Value),
			ct);
		var createdPost = await createBlogPostHandler.Handle(
			new CreateBlogPostCommand(
				"ObjectId workflow",
				"<p>Linked to category</p>",
				Author,
				IsPublished: true,
				CategoryId: createdCategory.Value),
			ct);
		var deleteAttempt = await deleteCategoryHandler.Handle(
			new DeleteCategoryCommand(createdCategory.Value),
			ct);

		// Assert
		createdCategory.Success.Should().BeTrue();
		createdCategory.Value.Should().NotBe(ObjectId.Empty);
		loadedCategory.Success.Should().BeTrue();
		loadedCategory.Value.Should().NotBeNull();
		loadedCategory.Value!.Id.Should().Be(createdCategory.Value);
		createdPost.Success.Should().BeTrue();
		deleteAttempt.Failure.Should().BeTrue();
		deleteAttempt.ErrorCode.Should().Be(ResultErrorCode.Conflict);
		deleteAttempt.Error.Should().Contain("Technology");

		var categoryStillExists = await categoryRepo.GetByIdAsync(createdCategory.Value, ct);
		categoryStillExists.Should().NotBeNull();
		categoryStillExists!.Id.Should().Be(createdCategory.Value);
	}
}
