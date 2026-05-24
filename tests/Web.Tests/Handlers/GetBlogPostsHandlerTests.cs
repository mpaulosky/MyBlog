//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     GetBlogPostsHandlerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using MyBlog.Web.Features.BlogPosts.List;

namespace Web.Handlers;

public class GetBlogPostsHandlerTests
{
	private readonly IBlogPostRepository _repo = Substitute.For<IBlogPostRepository>();
	private readonly IBlogPostCacheService _cache = Substitute.For<IBlogPostCacheService>();
	private readonly GetBlogPostsHandler _handler;

	public GetBlogPostsHandlerTests()
	{
		_handler = new GetBlogPostsHandler(_repo, _cache);
	}

	private static List<BlogPostDto> MakeDtos() =>
	[
	new(ObjectId.GenerateNewId(), "T1", "C1", string.Empty, "A1", string.Empty, [], DateTime.UtcNow, null, false, ObjectId.GenerateNewId()),
	new(ObjectId.GenerateNewId(), "T2", "C2", string.Empty, "A2", string.Empty, [], DateTime.UtcNow, null, true, null),
	];

	[Fact]
	public async Task Handle_L1CacheHit_ReturnsCachedDataWithoutCallingRepo()
	{
		// Arrange
		var cachedList = MakeDtos();
		_cache.GetOrFetchAllAsync(
		Arg.Any<Func<Task<IReadOnlyList<BlogPostDto>>>>(),
		Arg.Any<CancellationToken>())
		.Returns(new ValueTask<IReadOnlyList<BlogPostDto>>(cachedList));

		// Act
		var result = await _handler.Handle(new GetBlogPostsQuery(), CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(2);
		result.Value.Should().BeEquivalentTo(cachedList);
		await _repo.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_L2CacheHit_ReturnsCachedDataWithoutCallingRepo()
	{
		// Arrange
		var cachedList = MakeDtos();
		_cache.GetOrFetchAllAsync(
		Arg.Any<Func<Task<IReadOnlyList<BlogPostDto>>>>(),
		Arg.Any<CancellationToken>())
		.Returns(new ValueTask<IReadOnlyList<BlogPostDto>>(cachedList));

		// Act
		var result = await _handler.Handle(new GetBlogPostsQuery(), CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(2);
		result.Value.Should().BeEquivalentTo(cachedList);
		await _repo.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_CacheMiss_CallsRepoAndPopulatesBothCaches()
	{
		// Arrange
		var post1 = BlogPost.Create("T1", "C1", new PostAuthor("", "Test Author", "", []));
		var categoryId = ObjectId.GenerateNewId();
		post1.AssignCategory(categoryId);
		var post2 = BlogPost.Create("T2", "C2", new PostAuthor("", "Test Author", "", []));
		_repo.GetAllAsync(Arg.Any<CancellationToken>())
		.Returns(new List<BlogPost> { post1, post2 });
		_cache.GetOrFetchAllAsync(
		Arg.Any<Func<Task<IReadOnlyList<BlogPostDto>>>>(),
		Arg.Any<CancellationToken>())
		.Returns<ValueTask<IReadOnlyList<BlogPostDto>>>(ci =>
		{
			var fetch = ci.Arg<Func<Task<IReadOnlyList<BlogPostDto>>>>();
			return new ValueTask<IReadOnlyList<BlogPostDto>>(fetch().GetAwaiter().GetResult());
		});

		// Act
		var result = await _handler.Handle(new GetBlogPostsQuery(), CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(2);
		result.Value[0].Id.Should().Be(post1.Id);
		result.Value[0].CategoryId.Should().Be(categoryId);
		result.Value[1].Id.Should().Be(post2.Id);
		await _repo.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_RepoThrows_ReturnsFailResult()
	{
		// Arrange
		_cache.GetOrFetchAllAsync(
		Arg.Any<Func<Task<IReadOnlyList<BlogPostDto>>>>(),
		Arg.Any<CancellationToken>())
		.Returns(new ValueTask<IReadOnlyList<BlogPostDto>>(
		Task.FromException<IReadOnlyList<BlogPostDto>>(
		new InvalidOperationException("db error"))));

		// Act
		var result = await _handler.Handle(new GetBlogPostsQuery(), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("db error");
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

	[Fact]
	public async Task Handle_OperationCanceled_Rethrows()
	{
		// Arrange
		_cache.GetOrFetchAllAsync(
			Arg.Any<Func<Task<IReadOnlyList<BlogPostDto>>>>(),
			Arg.Any<CancellationToken>())
			.ThrowsAsync(new OperationCanceledException());

		// Act
		Func<Task> act = () => _handler.Handle(new GetBlogPostsQuery(), CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task Handle_UnexpectedException_ReturnsUnexpectedErrorResult()
	{
		// Arrange
		_cache.GetOrFetchAllAsync(
			Arg.Any<Func<Task<IReadOnlyList<BlogPostDto>>>>(),
			Arg.Any<CancellationToken>())
			.ThrowsAsync(new TimeoutException("db timeout"));

		// Act
		var result = await _handler.Handle(new GetBlogPostsQuery(), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Be("An unexpected error occurred.");
	}
}
