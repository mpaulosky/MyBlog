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
	private readonly PassThroughBlogPostCacheService _cache = new();
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
		_cache.GetOrFetchAllAsyncHandler = (_, _) => new ValueTask<IReadOnlyList<BlogPostDto>>(cachedList);

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
		_cache.GetOrFetchAllAsyncHandler = (_, _) => new ValueTask<IReadOnlyList<BlogPostDto>>(cachedList);

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
		_cache.GetOrFetchAllAsyncHandler = (fetch, _) => new ValueTask<IReadOnlyList<BlogPostDto>>(fetch());

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
		_repo.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Task.FromException<IReadOnlyList<BlogPost>>(
				new InvalidOperationException("db error")));

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
		_cache.GetOrFetchAllAsyncHandler = (_, _) => throw new InvalidOperationException("redis down");

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
		_cache.GetOrFetchAllAsyncHandler = (_, _) => throw new OperationCanceledException();

		// Act
		Func<Task> act = () => _handler.Handle(new GetBlogPostsQuery(), CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task Handle_UnexpectedException_ReturnsUnexpectedErrorResult()
	{
		// Arrange
		_cache.GetOrFetchAllAsyncHandler = (_, _) => throw new TimeoutException("db timeout");

		// Act
		var result = await _handler.Handle(new GetBlogPostsQuery(), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Be("An unexpected error occurred.");
	}

	private sealed class PassThroughBlogPostCacheService : IBlogPostCacheService
	{
		public Func<Func<Task<IReadOnlyList<BlogPostDto>>>, CancellationToken, ValueTask<IReadOnlyList<BlogPostDto>>> GetOrFetchAllAsyncHandler { get; set; } =
			(fetch, _) => new ValueTask<IReadOnlyList<BlogPostDto>>(fetch());

		public ValueTask<IReadOnlyList<BlogPostDto>> GetOrFetchAllAsync(
			Func<Task<IReadOnlyList<BlogPostDto>>> fetch,
			CancellationToken ct = default) =>
			GetOrFetchAllAsyncHandler(fetch, ct);

		public ValueTask<BlogPostDto?> GetOrFetchByIdAsync(
			ObjectId id,
			Func<Task<BlogPostDto?>> fetch,
			CancellationToken ct = default) =>
			throw new NotSupportedException();

		public Task InvalidateAllAsync(CancellationToken ct = default) => Task.CompletedTask;

		public Task InvalidateByIdAsync(ObjectId id, CancellationToken ct = default) => Task.CompletedTask;
	}
}
