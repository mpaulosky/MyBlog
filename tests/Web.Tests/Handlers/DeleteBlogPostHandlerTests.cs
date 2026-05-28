//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     DeleteBlogPostHandlerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using Microsoft.EntityFrameworkCore;

using MyBlog.Domain.Abstractions;
using MyBlog.Web.Features.BlogPosts.Delete;

namespace Web.Handlers;

public class DeleteBlogPostHandlerTests
{
	private readonly IBlogPostRepository _repo = Substitute.For<IBlogPostRepository>();
	private readonly IBlogPostCacheService _cache = Substitute.For<IBlogPostCacheService>();
	private readonly DeleteBlogPostHandler _handler;

	public DeleteBlogPostHandlerTests()
	{
		_handler = new DeleteBlogPostHandler(_repo, _cache);
	}

	[Fact]
	public async Task Handle_Success_DeletesAndInvalidatesBothCaches()
	{
		// Arrange
		var id = ObjectId.GenerateNewId();
		var command = new DeleteBlogPostCommand(id);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		await _repo.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
		await _cache.Received(1).InvalidateAllAsync(Arg.Any<CancellationToken>());
		await _cache.Received(1).InvalidateByIdAsync(id, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_RepoThrows_ReturnsFailResult()
	{
		// Arrange
		var id = ObjectId.GenerateNewId();
		var command = new DeleteBlogPostCommand(id);
		_repo.DeleteAsync(id, Arg.Any<CancellationToken>())
		.ThrowsAsync(new InvalidOperationException("delete failed"));

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("delete failed");
	}

	[Fact]
	public async Task Handle_ConcurrentDelete_ReturnsConcurrencyErrorCode()
	{
		// Arrange
		var id = ObjectId.GenerateNewId();
		var command = new DeleteBlogPostCommand(id);
		_repo.DeleteAsync(id, Arg.Any<CancellationToken>())
		.ThrowsAsync(new DbUpdateConcurrencyException("conflict", new InvalidOperationException("stale delete state")));

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.Concurrency);
	}

	[Fact]
	public async Task Handle_OperationCanceled_Rethrows()
	{
		// Arrange
		var id = ObjectId.GenerateNewId();
		var command = new DeleteBlogPostCommand(id);
		_repo.DeleteAsync(id, Arg.Any<CancellationToken>())
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
		var id = ObjectId.GenerateNewId();
		var command = new DeleteBlogPostCommand(id);
		_repo.DeleteAsync(id, Arg.Any<CancellationToken>())
			.ThrowsAsync(new TimeoutException("db timeout"));

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Be("An unexpected error occurred.");
	}
}
