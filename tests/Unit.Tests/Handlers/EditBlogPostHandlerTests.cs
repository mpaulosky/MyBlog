//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     EditBlogPostHandlerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Unit.Tests
//=======================================================

// TDD-red: these tests will not compile until #110 refactors EditBlogPostHandler
// to accept IBlogPostCacheService instead of IMemoryCache + IDistributedCache.
// EditBlogPostHandler handles both EditBlogPostCommand and GetBlogPostByIdQuery.
// GetBlogPostByIdQuery tests live in GetBlogPostByIdHandlerTests.cs.

using MyBlog.Web.Features.BlogPosts.Edit;

namespace Unit.Handlers;

public class EditBlogPostHandlerTests
{
	private readonly IBlogPostRepository _repo = Substitute.For<IBlogPostRepository>();
	private readonly IBlogPostCacheService _cache = Substitute.For<IBlogPostCacheService>();
	private readonly EditBlogPostHandler _handler;

	public EditBlogPostHandlerTests()
	{
		_handler = new EditBlogPostHandler(_repo, _cache);
	}

	[Fact]
	public async Task Handle_Success_UpdatesPostAndInvalidatesBothCacheKeys()
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
		// Must invalidate both the per-post key and the "all" list
		await _cache.Received(1).InvalidateByIdAsync(post.Id, Arg.Any<CancellationToken>());
		await _cache.Received(1).InvalidateAllAsync(Arg.Any<CancellationToken>());
		post.Title.Should().Be("New Title");
		post.Content.Should().Be("New Content");
	}

	[Fact]
	public async Task Handle_NotFound_ReturnsFailResult()
	{
		// Arrange
		var id = Guid.NewGuid();
		_repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((BlogPost?)null);

		// Act
		var result = await _handler.Handle(new EditBlogPostCommand(id, "T", "C"), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain(id.ToString());
		await _cache.DidNotReceive().InvalidateByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
		await _cache.DidNotReceive().InvalidateAllAsync(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_ConcurrentUpdate_ReturnsConcurrencyErrorCode()
	{
		// Arrange
		var post = BlogPost.Create("Title", "Content", "Author");
		_repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
		_repo.UpdateAsync(Arg.Any<BlogPost>(), Arg.Any<CancellationToken>())
			.ThrowsAsync(new DbUpdateConcurrencyException("conflict", new Exception()));

		// Act
		var result = await _handler.Handle(
			new EditBlogPostCommand(post.Id, "New Title", "New Content"),
			CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.Concurrency);
	}
}
