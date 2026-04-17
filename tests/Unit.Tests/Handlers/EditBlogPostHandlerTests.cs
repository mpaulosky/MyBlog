using System.Text.Json;
using Domain.Abstractions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using MyBlog.Domain.Entities;
using MyBlog.Domain.Interfaces;
using MyBlog.Web.Data;
using MyBlog.Web.Features.BlogPosts.Edit;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MyBlog.Unit.Tests.Handlers;

public class EditBlogPostHandlerTests
{
    private readonly IBlogPostRepository _repo = Substitute.For<IBlogPostRepository>();
    private readonly IMemoryCache _localCache = Substitute.For<IMemoryCache>();
    private readonly IDistributedCache _distributedCache = Substitute.For<IDistributedCache>();
    private readonly ICacheEntry _cacheEntry = Substitute.For<ICacheEntry>();
    private readonly EditBlogPostHandler _handler;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public EditBlogPostHandlerTests()
    {
        // IMemoryCache.Set<T> is an extension that calls CreateEntry — mock it so Set doesn't throw
        _localCache.CreateEntry(Arg.Any<object>()).Returns(_cacheEntry);
        _handler = new EditBlogPostHandler(_repo, _localCache, _distributedCache);
    }

    // ── Edit tests ────────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleEdit_Success_UpdatesPostAndInvalidatesBothCaches()
    {
        // Arrange
        var post = BlogPost.Create("Old Title", "Old Content", "Author");
        var command = new EditBlogPostCommand(post.Id, "New Title", "New Content");
        _repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        await _repo.Received(1).UpdateAsync(post, Arg.Any<CancellationToken>());
        _localCache.Received(1).Remove("blog:all");
        _localCache.Received(1).Remove($"blog:{post.Id}");
        await _distributedCache.Received(1).RemoveAsync("blog:all", Arg.Any<CancellationToken>());
        await _distributedCache.Received(1).RemoveAsync($"blog:{post.Id}", Arg.Any<CancellationToken>());
        post.Title.Should().Be("New Title");
        post.Content.Should().Be("New Content");
    }

    [Fact]
    public async Task HandleEdit_NotFound_ReturnsFailResult()
    {
        // Arrange
        var id = Guid.NewGuid();
        var command = new EditBlogPostCommand(id, "T", "C");
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((BlogPost?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Failure.Should().BeTrue();
        result.Error.Should().Contain(id.ToString());
    }

    // ── GetById tests ─────────────────────────────────────────────────────────

    [Fact]
    public async Task HandleGetById_L1CacheHit_ReturnsCachedDtoWithoutRepo()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new BlogPostDto(id, "T", "C", "A", DateTime.UtcNow, null, false);
        object? outVal = null;
        _localCache.TryGetValue(Arg.Any<object>(), out outVal)
            .Returns(x => { x[1] = (object)dto; return true; });

        // Act
        var result = await _handler.Handle(new GetBlogPostByIdQuery(id), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(id);
        await _repo.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleGetById_CacheMissRepoReturnsNull_ReturnsOkWithNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        object? outVal = null;
        _localCache.TryGetValue(Arg.Any<object>(), out outVal).Returns(false);
        _distributedCache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<byte[]?>(null));
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((BlogPost?)null);

        // Act
        var result = await _handler.Handle(new GetBlogPostByIdQuery(id), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task HandleGetById_CacheMissRepoReturnsPost_MapsToDtoAndCachesBoth()
    {
        // Arrange
        var post = BlogPost.Create("Title", "Content", "Author");
        object? outVal = null;
        _localCache.TryGetValue(Arg.Any<object>(), out outVal).Returns(false);
        _distributedCache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<byte[]?>(null));
        _repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);

        // Act
        var result = await _handler.Handle(new GetBlogPostByIdQuery(post.Id), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Title");
        // IMemoryCache.Set<T> calls CreateEntry — verify L1 was populated
        _localCache.Received(1).CreateEntry(Arg.Any<object>());
        await _distributedCache.Received(1).SetAsync(
            Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleEdit_ConcurrentUpdate_ReturnsConcurrencyErrorCode()
    {
        // Arrange
        var post = BlogPost.Create("Title", "Content", "Author");
        var command = new EditBlogPostCommand(post.Id, "New Title", "New Content");
        _repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);
        _repo.UpdateAsync(Arg.Any<BlogPost>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DbUpdateConcurrencyException("conflict", new Exception()));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Failure.Should().BeTrue();
        result.ErrorCode.Should().Be(ResultErrorCode.Concurrency);
    }
}
