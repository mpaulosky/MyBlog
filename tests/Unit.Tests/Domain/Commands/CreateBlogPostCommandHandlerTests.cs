//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CreateBlogPostCommandHandlerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Unit.Tests
//=======================================================

using MyBlog.Domain.Features.BlogPosts.Commands.CreateBlogPost;

namespace MyBlog.Unit.Tests.Domain.Commands;

public class CreateBlogPostCommandHandlerTests
{
	private readonly IBlogPostRepository _repo = Substitute.For<IBlogPostRepository>();
	private readonly CreateBlogPostCommandHandler _handler;

	public CreateBlogPostCommandHandlerTests()
	{
		_handler = new CreateBlogPostCommandHandler(_repo);
	}

	[Fact]
	public async Task Handle_ValidCommand_AddsPostAndReturnsGuid()
	{
		// Arrange
		var command = new CreateBlogPostCommand("Test Title", "Test Content", "Author One");

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBe(Guid.Empty);
		await _repo.Received(1).AddAsync(
			Arg.Is<BlogPost>(p => p.Title == "Test Title" && p.Author == "Author One"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_RepositoryThrows_PropagatesException()
	{
		// Arrange
		var command = new CreateBlogPostCommand("Title", "Content", "Author");
		_repo.AddAsync(Arg.Any<BlogPost>(), Arg.Any<CancellationToken>())
			.ThrowsAsync(new InvalidOperationException("DB error"));

		// Act
		var act = () => _handler.Handle(command, CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>();
	}
}
