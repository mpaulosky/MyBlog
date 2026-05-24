//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CreateBlogPostHandlerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using Ganss.Xss;

using Microsoft.Extensions.Logging.Abstractions;

using MyBlog.Web.Features.BlogPosts.Create;

namespace Web.Handlers;

public class CreateBlogPostHandlerTests
{
	private readonly IBlogPostRepository _repo = Substitute.For<IBlogPostRepository>();
	private readonly IBlogPostCacheService _cache = Substitute.For<IBlogPostCacheService>();
	private readonly IHtmlSanitizer _sanitizer = Substitute.For<IHtmlSanitizer>();
	private readonly CreateBlogPostHandler _handler;

	public CreateBlogPostHandlerTests()
	{
		_sanitizer.Sanitize(Arg.Any<string>()).Returns(ci => ci.ArgAt<string>(0));
		_handler = new CreateBlogPostHandler(_repo, _cache, _sanitizer, NullLogger<CreateBlogPostHandler>.Instance);
	}

	[Fact]
	public async Task Handle_Success_CreatesPostInvalidatesCacheAndReturnsGuid()
	{
		// Arrange
		var command = new CreateBlogPostCommand("Title", "Content", new PostAuthor("", "Author", "", []));

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBe(ObjectId.Empty);
		await _repo.Received(1).AddAsync(Arg.Any<BlogPost>(), Arg.Any<CancellationToken>());
		await _cache.Received(1).InvalidateAllAsync(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_RepoThrows_ReturnsFailResult()
	{
		// Arrange
		var command = new CreateBlogPostCommand("Title", "Content", new PostAuthor("", "Author", "", []));
		_repo.AddAsync(Arg.Any<BlogPost>(), Arg.Any<CancellationToken>())
		.ThrowsAsync(new InvalidOperationException("insert failed"));

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("insert failed");
	}

	[Fact]
	public async Task Handle_Success_DoesNotCallInvalidateById()
	{
		// Arrange — create should only bust the "all" list, not a specific post key
		var command = new CreateBlogPostCommand("Title", "Content", new PostAuthor("", "Author", "", []));

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		await _cache.Received(1).InvalidateAllAsync(Arg.Any<CancellationToken>());
		await _cache.DidNotReceive().InvalidateByIdAsync(Arg.Any<ObjectId>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_OperationCanceled_Rethrows()
	{
		// Arrange
		var command = new CreateBlogPostCommand("Title", "Content", new PostAuthor("", "Author", "", []));
		_repo.AddAsync(Arg.Any<BlogPost>(), Arg.Any<CancellationToken>())
			.ThrowsAsync(new OperationCanceledException());

		// Act
		Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task Handle_UnexpectedException_ReturnsUnexpectedErrorResult()
	{
		// Arrange
		var command = new CreateBlogPostCommand("Title", "Content", new PostAuthor("", "Author", "", []));
		_repo.AddAsync(Arg.Any<BlogPost>(), Arg.Any<CancellationToken>())
			.ThrowsAsync(new TimeoutException("db timeout"));

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Be("An unexpected error occurred.");
	}

	[Fact]
	public async Task Handle_IsPublishedTrue_PersistsPublishedPost()
	{
		// Arrange
		BlogPost? persistedPost = null;
		var command = new CreateBlogPostCommand("Title", "Content", new PostAuthor("", "Author", "", []), true);
		_repo.AddAsync(Arg.Do<BlogPost>(post => persistedPost = post), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		persistedPost.Should().NotBeNull();
		persistedPost!.IsPublished.Should().BeTrue();
	}

	[Fact]
	public async Task Handle_DefaultIsPublishedFalse_PersistsUnpublishedPost()
	{
		// Arrange
		BlogPost? persistedPost = null;
		var command = new CreateBlogPostCommand("Title", "Content", new PostAuthor("", "Author", "", []));
		_repo.AddAsync(Arg.Do<BlogPost>(post => persistedPost = post), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		persistedPost.Should().NotBeNull();
		persistedPost!.IsPublished.Should().BeFalse();
	}
}
