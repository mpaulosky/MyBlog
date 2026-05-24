//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     EditBlogPostHandlerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using Ganss.Xss;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using MyBlog.Domain.Abstractions;
using MyBlog.Web.Features.BlogPosts.Edit;

namespace Web.Handlers;

public class EditBlogPostHandlerTests
{
	private readonly IBlogPostRepository _repo = Substitute.For<IBlogPostRepository>();
	private readonly IBlogPostCacheService _cache = Substitute.For<IBlogPostCacheService>();
	private readonly IHtmlSanitizer _sanitizer = Substitute.For<IHtmlSanitizer>();
	private readonly EditBlogPostHandler _handler;

	public EditBlogPostHandlerTests()
	{
		_sanitizer.Sanitize(Arg.Any<string>()).Returns(ci => ci.ArgAt<string>(0));
		_handler = new EditBlogPostHandler(_repo, _cache, _sanitizer, NullLogger<EditBlogPostHandler>.Instance);
	}

	// ── Edit tests ────────────────────────────────────────────────────────────

	[Fact]
	public async Task HandleEdit_Success_UpdatesPostAndInvalidatesBothCaches()
	{
		// Arrange
		var authorId = "auth0|author1";
		var post = BlogPost.Create("Old Title", "Old Content", new PostAuthor(authorId, "Test Author", "", []));
		var command = new EditBlogPostCommand(post.Id, "New Title", "New Content", authorId, false);
		_repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		await _repo.Received(1).UpdateAsync(post, Arg.Any<CancellationToken>());
		await _cache.Received(1).InvalidateAllAsync(Arg.Any<CancellationToken>());
		await _cache.Received(1).InvalidateByIdAsync(post.Id, Arg.Any<CancellationToken>());
		post.Title.Should().Be("New Title");
		post.Content.Should().Be("New Content");
	}

	[Fact]
	public async Task HandleEdit_CategoryIdProvided_AssignsCategoryBeforePersisting()
	{
		// Arrange
		var authorId = "auth0|author1";
		var categoryId = ObjectId.GenerateNewId();
		var post = BlogPost.Create("Old Title", "Old Content", new PostAuthor(authorId, "Test Author", "", []));
		var command = new EditBlogPostCommand(post.Id, "New Title", "New Content", authorId, false, CategoryId: categoryId);
		_repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		post.CategoryId.Should().Be(categoryId);
		await _repo.Received(1).UpdateAsync(post, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleEdit_ClearCategoryRequested_RemovesCategoryBeforePersisting()
	{
		// Arrange
		var authorId = "auth0|author1";
		var existingCategoryId = ObjectId.GenerateNewId();
		var post = BlogPost.Create("Old Title", "Old Content", new PostAuthor(authorId, "Test Author", "", []));
		post.AssignCategory(existingCategoryId);
		var command = new EditBlogPostCommand(post.Id, "New Title", "New Content", authorId, false, CategoryId: null, ClearCategory: true);
		_repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		post.CategoryId.Should().BeNull();
		await _repo.Received(1).UpdateAsync(post, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleEdit_AdminCanEditAnyPost_ReturnsSuccess()
	{
		// Arrange
		var post = BlogPost.Create("Title", "Content", new PostAuthor("auth0|authorX", "Author X", "", []));
		var command = new EditBlogPostCommand(post.Id, "New Title", "New Content", "auth0|adminUser", true);
		_repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
	}

	[Fact]
	public async Task HandleEdit_IsPublishedTrue_PublishesPost()
	{
		// Arrange
		var authorId = "auth0|author1";
		var post = BlogPost.Create("Title", "Content", new PostAuthor(authorId, "Author 1", "", []));
		var command = new EditBlogPostCommand(post.Id, "Updated Title", "Updated Content", authorId, false, true);
		_repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		post.IsPublished.Should().BeTrue();
	}

	[Fact]
	public async Task HandleEdit_IsPublishedFalse_UnpublishesPost()
	{
		// Arrange
		var authorId = "auth0|author1";
		var post = BlogPost.Create("Title", "Content", new PostAuthor(authorId, "Author 1", "", []));
		post.Publish();
		var command = new EditBlogPostCommand(post.Id, "Updated Title", "Updated Content", authorId, false, false);
		_repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		post.IsPublished.Should().BeFalse();
	}

	[Fact]
	public async Task HandleEdit_DifferentNonAdminUser_ReturnsUnauthorized()
	{
		// Arrange
		var post = BlogPost.Create("Title", "Content", new PostAuthor("auth0|authorX", "Author X", "", []));
		var command = new EditBlogPostCommand(post.Id, "New Title", "New Content", "auth0|differentUser", false);
		_repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(MyBlog.Domain.Abstractions.ResultErrorCode.Unauthorized);
		result.Error.Should().Contain("not authorized");
	}

	[Fact]
	public async Task HandleEdit_AuthorCanEditOwnPost_ReturnsSuccess()
	{
		// Arrange
		var authorId = "auth0|author1";
		var post = BlogPost.Create("Title", "Content", new PostAuthor(authorId, "Author 1", "", []));
		var command = new EditBlogPostCommand(post.Id, "Updated Title", "Updated Content", authorId, false);
		_repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
	}

	[Fact]
	public async Task HandleEdit_NotFound_ReturnsFailResult()
	{
		// Arrange
		var id = ObjectId.GenerateNewId();
		var command = new EditBlogPostCommand(id, "T", "C", "auth0|user1", false);
		_repo.GetByIdAsync(Arg.Is<ObjectId>(g => g == id), Arg.Any<CancellationToken>())
		.Returns((BlogPost?)null);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain(id.ToString());
	}

	// ── GetById tests ─────────────────────────────────────────────────────────

	[Fact]
	public async Task HandleGetById_L1CacheHit_ReturnsCachedDtoWithoutRepo()
	{
		// Arrange
		var id = ObjectId.GenerateNewId();
		var dto = new BlogPostDto(id, "T", "C", string.Empty, "A", string.Empty, [], DateTime.UtcNow, null, false, null);
		_cache.GetOrFetchByIdAsync(
		Arg.Any<ObjectId>(),
		Arg.Any<Func<Task<BlogPostDto?>>>(),
		Arg.Any<CancellationToken>())
		.Returns(new ValueTask<BlogPostDto?>(dto));

		// Act
		var result = await _handler.Handle(new GetBlogPostByIdQuery(id), CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Id.Should().Be(id);
		await _repo.DidNotReceive().GetByIdAsync(Arg.Any<ObjectId>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleGetById_CacheMissRepoReturnsNull_ReturnsOkWithNull()
	{
		// Arrange
		var id = ObjectId.GenerateNewId();
		_repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((BlogPost?)null);
		_cache.GetOrFetchByIdAsync(
		Arg.Any<ObjectId>(),
		Arg.Any<Func<Task<BlogPostDto?>>>(),
		Arg.Any<CancellationToken>())
		.Returns<ValueTask<BlogPostDto?>>(ci =>
		{
			var fetch = ci.Arg<Func<Task<BlogPostDto?>>>();
			return new ValueTask<BlogPostDto?>(fetch().GetAwaiter().GetResult());
		});

		// Act
		var result = await _handler.Handle(new GetBlogPostByIdQuery(id), CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeNull();
	}

	[Fact]
	public async Task HandleGetById_CacheMissRepoReturnsPost_MapsToDtoAndPopulatesCache()
	{
		// Arrange
		var post = BlogPost.Create("Title", "Content", new PostAuthor("", "Test Author", "", []));
		var categoryId = ObjectId.GenerateNewId();
		post.AssignCategory(categoryId);
		_repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
		_cache.GetOrFetchByIdAsync(
		Arg.Any<ObjectId>(),
		Arg.Any<Func<Task<BlogPostDto?>>>(),
		Arg.Any<CancellationToken>())
		.Returns<ValueTask<BlogPostDto?>>(ci =>
		{
			var fetch = ci.Arg<Func<Task<BlogPostDto?>>>();
			return new ValueTask<BlogPostDto?>(fetch().GetAwaiter().GetResult());
		});

		// Act
		var result = await _handler.Handle(new GetBlogPostByIdQuery(post.Id), CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Id.Should().Be(post.Id);
		result.Value!.Title.Should().Be("Title");
		result.Value.CategoryId.Should().Be(categoryId);
		await _repo.Received(1).GetByIdAsync(post.Id, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleEdit_ConcurrentUpdate_ReturnsConcurrencyErrorCode()
	{
		// Arrange
		var authorId = "auth0|author1";
		var post = BlogPost.Create("Title", "Content", new PostAuthor(authorId, "Test Author", "", []));
		var command = new EditBlogPostCommand(post.Id, "New Title", "New Content", authorId, false);
		_repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
		_repo.UpdateAsync(Arg.Any<BlogPost>(), Arg.Any<CancellationToken>())
		.ThrowsAsync(new DbUpdateConcurrencyException("conflict", new Exception()));

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.Concurrency);
	}

	[Fact]
	public async Task HandleGetById_CacheServiceThrows_ReturnsFailResult()
	{
		// Arrange
		var id = ObjectId.GenerateNewId();
		_cache.GetOrFetchByIdAsync(
		id,
		Arg.Any<Func<Task<BlogPostDto?>>>(),
		Arg.Any<CancellationToken>())
		.ThrowsAsync(new InvalidOperationException("redis down"));

		// Act
		var result = await _handler.Handle(new GetBlogPostByIdQuery(id), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("redis down");
	}

	[Fact]
	public async Task HandleEdit_OperationCanceled_Rethrows()
	{
		// Arrange
		var authorId = "auth0|author1";
		var post = BlogPost.Create("Title", "Content", new PostAuthor(authorId, "Test Author", "", []));
		var command = new EditBlogPostCommand(post.Id, "New Title", "New Content", authorId, false);
		_repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>())
			.ThrowsAsync(new OperationCanceledException());

		// Act
		Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task HandleEdit_UnexpectedException_ReturnsUnexpectedErrorResult()
	{
		// Arrange
		var authorId = "auth0|author1";
		var post = BlogPost.Create("Title", "Content", new PostAuthor(authorId, "Test Author", "", []));
		var command = new EditBlogPostCommand(post.Id, "New Title", "New Content", authorId, false);
		_repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>())
			.ThrowsAsync(new TimeoutException("db timeout"));

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Be("An unexpected error occurred.");
	}

	[Fact]
	public async Task HandleGetById_OperationCanceled_Rethrows()
	{
		// Arrange
		var id = ObjectId.GenerateNewId();
		_cache.GetOrFetchByIdAsync(
			id,
			Arg.Any<Func<Task<BlogPostDto?>>>(),
			Arg.Any<CancellationToken>())
			.ThrowsAsync(new OperationCanceledException());

		// Act
		Func<Task> act = () => _handler.Handle(new GetBlogPostByIdQuery(id), CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task HandleGetById_UnexpectedException_ReturnsUnexpectedErrorResult()
	{
		// Arrange
		var id = ObjectId.GenerateNewId();
		_cache.GetOrFetchByIdAsync(
			id,
			Arg.Any<Func<Task<BlogPostDto?>>>(),
			Arg.Any<CancellationToken>())
			.ThrowsAsync(new TimeoutException("db timeout"));

		// Act
		var result = await _handler.Handle(new GetBlogPostByIdQuery(id), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Be("An unexpected error occurred.");
	}
}
