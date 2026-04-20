//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     DeleteBlogPostCommandHandlerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Unit.Tests
//=======================================================

using MyBlog.Domain.Features.BlogPosts.Commands.DeleteBlogPost;

namespace MyBlog.Unit.Tests.Domain.Commands;

public class DeleteBlogPostCommandHandlerTests
{
	private readonly IBlogPostRepository _repo = Substitute.For<IBlogPostRepository>();
	private readonly DeleteBlogPostCommandHandler _handler;

	public DeleteBlogPostCommandHandlerTests()
	{
		_handler = new DeleteBlogPostCommandHandler(_repo);
	}

	[Fact]
	public async Task Handle_ValidId_DeletesPostAndReturnsSuccess()
	{
		// Arrange
		var id = Guid.NewGuid();
		var command = new DeleteBlogPostCommand(id);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		await _repo.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_RepositoryThrows_PropagatesException()
	{
		// Arrange
		var command = new DeleteBlogPostCommand(Guid.NewGuid());
		_repo.DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
			.ThrowsAsync(new InvalidOperationException("DB error"));

		// Act
		var act = () => _handler.Handle(command, CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>();
	}
}
