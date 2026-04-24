//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     GetBlogPostsHandlerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Unit.Tests
//=======================================================

// TDD-red: these tests will not compile until #110 refactors GetBlogPostsHandler
// to accept IBlogPostCacheService instead of IMemoryCache + IDistributedCache.

using MyBlog.Web.Features.BlogPosts.List;

namespace Unit.Handlers;

public class GetBlogPostsHandlerTests
{
	private readonly IBlogPostRepository _repo = Substitute.For<IBlogPostRepository>();
	private readonly IBlogPostCacheService _cache = Substitute.For<IBlogPostCacheService>();
	private readonly GetBlogPostsHandler _handler;

	public GetBlogPostsHandlerTests()
	{
		_handler = new GetBlogPostsHandler(_repo, _cache);
	}

	private static IReadOnlyList<BlogPostDto> MakeDtos() =>
	[
		new(Guid.NewGuid(), "T1", "C1", "A1", DateTime.UtcNow, null, false),
		new(Guid.NewGuid(), "T2", "C2", "A2", DateTime.UtcNow, null, true),
	];

	[Fact]
	public async Task Handle_AlwaysDelegatesToCacheService_AndReturnsResult()
	{
		// Arrange
		var dtos = MakeDtos();
		_cache.GetOrFetchAllAsync(
				Arg.Any<Func<Task<IReadOnlyList<BlogPostDto>>>>(),
				Arg.Any<CancellationToken>())
			.Returns(new ValueTask<IReadOnlyList<BlogPostDto>>(dtos));

		// Act
		var result = await _handler.Handle(new GetBlogPostsQuery(), CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(2);
		await _cache.Received(1).GetOrFetchAllAsync(
			Arg.Any<Func<Task<IReadOnlyList<BlogPostDto>>>>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_FetchDelegateInvokedOnCacheMiss_CallsRepo()
	{
		// Arrange
		var post1 = BlogPost.Create("T1", "C1", "A1");
		var post2 = BlogPost.Create("T2", "C2", "A2");
		_repo.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(new List<BlogPost> { post1, post2 });

		// Simulate cache miss: invoke the fetch delegate passed by the handler
		_cache.GetOrFetchAllAsync(
				Arg.Any<Func<Task<IReadOnlyList<BlogPostDto>>>>(),
				Arg.Any<CancellationToken>())
			.Returns(async callInfo =>
			{
				var fetchFn = callInfo.ArgAt<Func<Task<IReadOnlyList<BlogPostDto>>>>(0);
				return await fetchFn();
			});

		// Act
		var result = await _handler.Handle(new GetBlogPostsQuery(), CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(2);
		await _repo.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_CacheServiceThrows_ReturnsFailResult()
	{
		// Arrange
		_cache.GetOrFetchAllAsync(
				Arg.Any<Func<Task<IReadOnlyList<BlogPostDto>>>>(),
				Arg.Any<CancellationToken>())
			.ThrowsAsync(new InvalidOperationException("redis down"));

		// Act
		var result = await _handler.Handle(new GetBlogPostsQuery(), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("redis down");
	}
}
