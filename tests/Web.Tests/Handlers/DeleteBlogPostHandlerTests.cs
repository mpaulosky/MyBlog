//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     DeleteBlogPostHandlerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Unit.Tests
//=======================================================

using Microsoft.EntityFrameworkCore;

using MyBlog.Domain.Abstractions;
using MyBlog.Web.Features.BlogPosts.Delete;

namespace Web.Handlers;

public class DeleteBlogPostHandlerTests
{
	private readonly IBlogPostRepository _repo = Substitute.For<IBlogPostRepository>();
	private readonly IMemoryCache _localCache = Substitute.For<IMemoryCache>();
	private readonly IDistributedCache _distributedCache = Substitute.For<IDistributedCache>();
	private readonly DeleteBlogPostHandler _handler;

	public DeleteBlogPostHandlerTests()
	{
		_handler = new DeleteBlogPostHandler(_repo, _localCache, _distributedCache);
	}

	[Fact]
	public async Task Handle_Success_DeletesAndInvalidatesBothCaches()
	{
		// Arrange
		var id = Guid.NewGuid();
		var command = new DeleteBlogPostCommand(id);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		await _repo.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
		_localCache.Received(1).Remove("blog:all");
		_localCache.Received(1).Remove($"blog:{id}");
		await _distributedCache.Received(1).RemoveAsync("blog:all", Arg.Any<CancellationToken>());
		await _distributedCache.Received(1).RemoveAsync($"blog:{id}", Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_RepoThrows_ReturnsFailResult()
	{
		// Arrange
		var id = Guid.NewGuid();
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
		var id = Guid.NewGuid();
		var command = new DeleteBlogPostCommand(id);
		_repo.DeleteAsync(id, Arg.Any<CancellationToken>())
				.ThrowsAsync(new DbUpdateConcurrencyException("conflict", new Exception()));

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.Concurrency);
	}
}
