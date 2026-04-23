//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     UpdateBlogPostCommandHandlerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Unit.Tests
//=======================================================

using MyBlog.Domain.Abstractions;
using MyBlog.Domain.Features.BlogPosts.Commands.UpdateBlogPost;

namespace Domain.Domain.Commands;

public class UpdateBlogPostCommandHandlerTests
{
	private readonly IBlogPostRepository _repo = Substitute.For<IBlogPostRepository>();
	private readonly UpdateBlogPostCommandHandler _handler;

	public UpdateBlogPostCommandHandlerTests()
	{
		_handler = new UpdateBlogPostCommandHandler(_repo);
	}

	[Fact]
	public async Task Handle_ExistingPost_UpdatesAndReturnsSuccess()
	{
		// Arrange
		var post = BlogPost.Create("Old Title", "Old Content", "Author");
		var command = new UpdateBlogPostCommand(post.Id, "New Title", "New Content");
		_repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		await _repo.Received(1).UpdateAsync(
			Arg.Is<BlogPost>(p => p.Title == "New Title"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_PostNotFound_ReturnsNotFoundFailure()
	{
		// Arrange
		var id = Guid.NewGuid();
		var command = new UpdateBlogPostCommand(id, "Title", "Content");
		_repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((BlogPost?)null);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
		await _repo.DidNotReceive().UpdateAsync(Arg.Any<BlogPost>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_RepositoryThrowsOnGet_PropagatesException()
	{
		// Arrange
		var command = new UpdateBlogPostCommand(Guid.NewGuid(), "Title", "Content");
		_repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
			.ThrowsAsync(new InvalidOperationException("DB error"));

		// Act
		var act = () => _handler.Handle(command, CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>();
	}
}
