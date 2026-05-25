//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     BlogPostHandlerObjectIdWorkflowTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests.Integration
//=======================================================

using Ganss.Xss;

using Microsoft.Extensions.Logging.Abstractions;

using MyBlog.Web.Features.BlogPosts.Create;
using MyBlog.Web.Features.BlogPosts.Delete;
using MyBlog.Web.Features.BlogPosts.Edit;
using MyBlog.Web.Features.BlogPosts.List;

using Web.Infrastructure;

namespace Web.BlogPosts;

[Collection("BlogPostIntegration")]
public sealed class BlogPostHandlerObjectIdWorkflowTests(MongoDbFixture fixture)
{
	private static readonly PostAuthor Author = new("auth0|author-1", "Author One", "author@example.com", ["Author"]);

	[Fact]
	public async Task BlogPostHandlers_RoundTrip_ObjectIds_Through_Real_Mongo_Workflow()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		var dbName = $"T{Guid.NewGuid():N}";
		var categoryRepo = new MongoDbCategoryRepository(fixture.CreateFactory(dbName));
		var blogPostRepo = new MongoDbBlogPostRepository(fixture.CreateFactory(dbName));
		var cache = new PassthroughBlogPostCacheService();
		var sanitizer = new HtmlSanitizer();
		var createHandler = new CreateBlogPostHandler(
			blogPostRepo,
			cache,
			sanitizer,
			NullLogger<CreateBlogPostHandler>.Instance);
		var editHandler = new EditBlogPostHandler(
			blogPostRepo,
			cache,
			sanitizer,
			NullLogger<EditBlogPostHandler>.Instance);
		var listHandler = new GetBlogPostsHandler(blogPostRepo, cache);
		var deleteHandler = new DeleteBlogPostHandler(blogPostRepo, cache);

		var initialCategory = Category.Create("Technology", "Tech posts.");
		await categoryRepo.AddAsync(initialCategory, ct);

		// Act
		var created = await createHandler.Handle(
			new CreateBlogPostCommand(
				"ObjectId flow",
				"<script>alert('x')</script><p>Safe content</p>",
				Author,
				IsPublished: true,
				CategoryId: initialCategory.Id),
			ct);
		var loaded = await editHandler.Handle(new GetBlogPostByIdQuery(created.Value), ct);
		var listed = await listHandler.Handle(new GetBlogPostsQuery(), ct);
		var updated = await editHandler.Handle(
			new EditBlogPostCommand(
				created.Value,
				"Updated ObjectId flow",
				"<p>Updated content</p>",
				Author.Id,
				CallerIsAdmin: false,
				IsPublished: false),
			ct);
		var reloaded = await editHandler.Handle(new GetBlogPostByIdQuery(created.Value), ct);
		var deleted = await deleteHandler.Handle(new DeleteBlogPostCommand(created.Value), ct);
		var afterDelete = await editHandler.Handle(new GetBlogPostByIdQuery(created.Value), ct);

		// Assert
		created.Success.Should().BeTrue();
		created.Value.Should().NotBe(ObjectId.Empty);
		loaded.Success.Should().BeTrue();
		loaded.Value.Should().NotBeNull();
		loaded.Value!.Id.Should().Be(created.Value);
		loaded.Value.CategoryId.Should().Be(initialCategory.Id);
		loaded.Value.Content.Should().Be("<p>Safe content</p>");

		listed.Success.Should().BeTrue();
		listed.Value.Should().ContainSingle(post => post.Id == created.Value && post.CategoryId == initialCategory.Id);

		updated.Success.Should().BeTrue();
		reloaded.Success.Should().BeTrue();
		reloaded.Value.Should().NotBeNull();
		reloaded.Value!.Id.Should().Be(created.Value);
		reloaded.Value.Title.Should().Be("Updated ObjectId flow");
		reloaded.Value.CategoryId.Should().Be(initialCategory.Id);
		reloaded.Value.IsPublished.Should().BeFalse();

		deleted.Success.Should().BeTrue();
		afterDelete.Success.Should().BeTrue();
		afterDelete.Value.Should().BeNull();
	}
}
