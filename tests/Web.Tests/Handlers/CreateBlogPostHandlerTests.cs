//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CreateBlogPostHandlerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using MyBlog.Web.Features.BlogPosts.Create;

namespace Web.Handlers;

public class CreateBlogPostHandlerTests
{
private readonly IBlogPostRepository _repo = Substitute.For<IBlogPostRepository>();
private readonly IBlogPostCacheService _cache = Substitute.For<IBlogPostCacheService>();
private readonly CreateBlogPostHandler _handler;

public CreateBlogPostHandlerTests()
{
_handler = new CreateBlogPostHandler(_repo, _cache);
}

[Fact]
public async Task Handle_Success_CreatesPostInvalidatesCacheAndReturnsGuid()
{
// Arrange
var command = new CreateBlogPostCommand("Title", "Content", "Author");

// Act
var result = await _handler.Handle(command, CancellationToken.None);

// Assert
result.Success.Should().BeTrue();
result.Value.Should().NotBeEmpty();
await _repo.Received(1).AddAsync(Arg.Any<BlogPost>(), Arg.Any<CancellationToken>());
await _cache.Received(1).InvalidateAllAsync(Arg.Any<CancellationToken>());
}

[Fact]
public async Task Handle_RepoThrows_ReturnsFailResult()
{
// Arrange
var command = new CreateBlogPostCommand("Title", "Content", "Author");
_repo.AddAsync(Arg.Any<BlogPost>(), Arg.Any<CancellationToken>())
.ThrowsAsync(new InvalidOperationException("insert failed"));

// Act
var result = await _handler.Handle(command, CancellationToken.None);

// Assert
result.Failure.Should().BeTrue();
result.Error.Should().Contain("insert failed");
}
}
