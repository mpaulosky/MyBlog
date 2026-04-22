//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     GetBlogPostByIdQueryHandlerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Unit.Tests
//=======================================================

using MyBlog.Domain.Abstractions;
using MyBlog.Domain.Features.BlogPosts.Queries.GetBlogPostById;

namespace Domain.Domain.Queries;

public class GetBlogPostByIdQueryHandlerTests
{
	private readonly IBlogPostRepository _repo = Substitute.For<IBlogPostRepository>();
	private readonly GetBlogPostByIdQueryHandler _handler;

	public GetBlogPostByIdQueryHandlerTests()
	{
		_handler = new GetBlogPostByIdQueryHandler(_repo);
	}

	[Fact]
	public async Task Handle_ExistingPost_ReturnsSuccessWithPost()
	{
		// Arrange
		var post = BlogPost.Create("Title", "Content", "Author");
		var query = new GetBlogPostByIdQuery(post.Id);
		_repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeEquivalentTo(post);
	}

	[Fact]
	public async Task Handle_PostNotFound_ReturnsNotFoundFailure()
	{
		// Arrange
		var id = Guid.NewGuid();
		var query = new GetBlogPostByIdQuery(id);
		_repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((BlogPost?)null);

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public async Task Handle_RepositoryThrows_PropagatesException()
	{
		// Arrange
		var query = new GetBlogPostByIdQuery(Guid.NewGuid());
		_repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
			.ThrowsAsync(new InvalidOperationException("DB error"));

		// Act
		var act = () => _handler.Handle(query, CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>();
	}
}
