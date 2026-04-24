//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     GetBlogPostByIdHandlerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Unit.Tests
//=======================================================

// TDD-red: these tests will not compile until #110 refactors EditBlogPostHandler
// to accept IBlogPostCacheService instead of IMemoryCache + IDistributedCache.
// EditBlogPostHandler handles both EditBlogPostCommand and GetBlogPostByIdQuery.

using MyBlog.Web.Features.BlogPosts.Edit;

namespace Unit.Handlers;

public class GetBlogPostByIdHandlerTests
{
	private readonly IBlogPostRepository _repo = Substitute.For<IBlogPostRepository>();
	private readonly IBlogPostCacheService _cache = Substitute.For<IBlogPostCacheService>();
	private readonly EditBlogPostHandler _handler;

	public GetBlogPostByIdHandlerTests()
	{
		_handler = new EditBlogPostHandler(_repo, _cache);
	}

	[Fact]
	public async Task Handle_DelegatesToCacheWithCorrectId_AndReturnsDto()
	{
		// Arrange
		var id = Guid.NewGuid();
		var dto = new BlogPostDto(id, "Title", "Content", "Author", DateTime.UtcNow, null, false);
		_cache.GetOrFetchByIdAsync(
				id,
				Arg.Any<Func<Task<BlogPostDto?>>>(),
				Arg.Any<CancellationToken>())
			.Returns(new ValueTask<BlogPostDto?>(dto));

		// Act
		var result = await _handler.Handle(new GetBlogPostByIdQuery(id), CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Id.Should().Be(id);
		await _cache.Received(1).GetOrFetchByIdAsync(
			id,
			Arg.Any<Func<Task<BlogPostDto?>>>(),
			Arg.Any<CancellationToken>());
		await _repo.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_FetchDelegateInvokedOnCacheMiss_CallsRepo()
	{
		// Arrange
		var post = BlogPost.Create("Title", "Content", "Author");
		_repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);

		// Simulate cache miss: invoke the fetch delegate passed by the handler
		_cache.GetOrFetchByIdAsync(
				post.Id,
				Arg.Any<Func<Task<BlogPostDto?>>>(),
				Arg.Any<CancellationToken>())
			.Returns(callInfo => new ValueTask<BlogPostDto?>(
				callInfo.ArgAt<Func<Task<BlogPostDto?>>>(1)()));

		// Act
		var result = await _handler.Handle(new GetBlogPostByIdQuery(post.Id), CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Title.Should().Be("Title");
		await _repo.Received(1).GetByIdAsync(post.Id, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_CacheMissRepoReturnsNull_ReturnsOkWithNull()
	{
		// Arrange
		var id = Guid.NewGuid();
		_repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((BlogPost?)null);

		_cache.GetOrFetchByIdAsync(
				id,
				Arg.Any<Func<Task<BlogPostDto?>>>(),
				Arg.Any<CancellationToken>())
			.Returns(callInfo => new ValueTask<BlogPostDto?>(
				callInfo.ArgAt<Func<Task<BlogPostDto?>>>(1)()));

		// Act
		var result = await _handler.Handle(new GetBlogPostByIdQuery(id), CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeNull();
	}

	[Fact]
	public async Task Handle_CacheServiceThrows_ReturnsFailResult()
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
