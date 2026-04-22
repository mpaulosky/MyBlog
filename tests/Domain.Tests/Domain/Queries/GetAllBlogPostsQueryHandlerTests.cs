//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     GetAllBlogPostsQueryHandlerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Unit.Tests
//=======================================================

using MyBlog.Domain.Features.BlogPosts.Queries.GetAllBlogPosts;

namespace Domain.Domain.Queries;

public class GetAllBlogPostsQueryHandlerTests
{
	private readonly IBlogPostRepository _repo = Substitute.For<IBlogPostRepository>();
	private readonly GetAllBlogPostsQueryHandler _handler;

	public GetAllBlogPostsQueryHandlerTests()
	{
		_handler = new GetAllBlogPostsQueryHandler(_repo);
	}

	[Fact]
	public async Task Handle_WithPosts_ReturnsSuccessWithList()
	{
		// Arrange
		var posts = new List<BlogPost>
		{
			BlogPost.Create("Post 1", "Content 1", "Author A"),
			BlogPost.Create("Post 2", "Content 2", "Author B")
		};
		_repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyList<BlogPost>)posts);

		// Act
		var result = await _handler.Handle(new GetAllBlogPostsQuery(), CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(2);
	}

	[Fact]
	public async Task Handle_EmptyRepository_ReturnsSuccessWithEmptyList()
	{
		// Arrange
		_repo.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns((IReadOnlyList<BlogPost>)new List<BlogPost>());

		// Act
		var result = await _handler.Handle(new GetAllBlogPostsQuery(), CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task Handle_RepositoryThrows_PropagatesException()
	{
		// Arrange
		_repo.GetAllAsync(Arg.Any<CancellationToken>())
			.ThrowsAsync(new InvalidOperationException("DB error"));

		// Act
		var act = () => _handler.Handle(new GetAllBlogPostsQuery(), CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>();
	}
}
