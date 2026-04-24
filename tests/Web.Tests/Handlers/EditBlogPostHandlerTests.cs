//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     EditBlogPostHandlerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using Microsoft.EntityFrameworkCore;

using MyBlog.Domain.Abstractions;
using MyBlog.Web.Features.BlogPosts.Edit;

namespace Web.Handlers;

public class EditBlogPostHandlerTests
{
	private readonly IBlogPostRepository _repo = Substitute.For<IBlogPostRepository>();
	private readonly IBlogPostCacheService _cache = Substitute.For<IBlogPostCacheService>();
	private readonly EditBlogPostHandler _handler;

	public EditBlogPostHandlerTests()
	{
		_handler = new EditBlogPostHandler(_repo, _cache);
	}

	// ── Edit tests ────────────────────────────────────────────────────────────

	[Fact]
	public async Task HandleEdit_Success_UpdatesPostAndInvalidatesBothCaches()
	{
		// Arrange
		var post = BlogPost.Create("Old Title", "Old Content", "Author");
		var command = new EditBlogPostCommand(post.Id, "New Title", "New Content");
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
	public async Task HandleEdit_NotFound_ReturnsFailResult()
	{
		// Arrange
		var id = Guid.NewGuid();
		var command = new EditBlogPostCommand(id, "T", "C");
		_repo.GetByIdAsync(Arg.Is<Guid>(g => g == id), Arg.Any<CancellationToken>())
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
		var id = Guid.NewGuid();
		var dto = new BlogPostDto(id, "T", "C", "A", DateTime.UtcNow, null, false);
		_cache.GetOrFetchByIdAsync(
		Arg.Any<Guid>(),
		Arg.Any<Func<Task<BlogPostDto?>>>(),
		Arg.Any<CancellationToken>())
		.Returns(new ValueTask<BlogPostDto?>(dto));

		// Act
		var result = await _handler.Handle(new GetBlogPostByIdQuery(id), CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Id.Should().Be(id);
		await _repo.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleGetById_CacheMissRepoReturnsNull_ReturnsOkWithNull()
	{
		// Arrange
		var id = Guid.NewGuid();
		_repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((BlogPost?)null);
		_cache.GetOrFetchByIdAsync(
		Arg.Any<Guid>(),
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
		var post = BlogPost.Create("Title", "Content", "Author");
		_repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
		_cache.GetOrFetchByIdAsync(
		Arg.Any<Guid>(),
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
		result.Value!.Title.Should().Be("Title");
		await _repo.Received(1).GetByIdAsync(post.Id, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task HandleEdit_ConcurrentUpdate_ReturnsConcurrencyErrorCode()
	{
		// Arrange
		var post = BlogPost.Create("Title", "Content", "Author");
		var command = new EditBlogPostCommand(post.Id, "New Title", "New Content");
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
		var id = Guid.NewGuid();
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
}
