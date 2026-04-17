using System.Text.Json;
using Domain.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using MyBlog.Domain.Entities;
using MyBlog.Domain.Interfaces;
using MyBlog.Web.Data;
using MyBlog.Web.Features.BlogPosts.List;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace MyBlog.Unit.Tests.Handlers;

public class GetBlogPostsHandlerTests
{
    private readonly IBlogPostRepository _repo = Substitute.For<IBlogPostRepository>();
    private readonly IMemoryCache _localCache = Substitute.For<IMemoryCache>();
    private readonly IDistributedCache _distributedCache = Substitute.For<IDistributedCache>();
    private readonly ICacheEntry _cacheEntry = Substitute.For<ICacheEntry>();
    private readonly GetBlogPostsHandler _handler;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public GetBlogPostsHandlerTests()
    {
        // IMemoryCache.Set<T> is an extension that calls CreateEntry — mock it so Set doesn't throw
        _localCache.CreateEntry(Arg.Any<object>()).Returns(_cacheEntry);
        _handler = new GetBlogPostsHandler(_repo, _localCache, _distributedCache);
    }

    private static List<BlogPostDto> MakeDtos() =>
    [
        new(Guid.NewGuid(), "T1", "C1", "A1", DateTime.UtcNow, null, false),
        new(Guid.NewGuid(), "T2", "C2", "A2", DateTime.UtcNow, null, true),
    ];

    [Fact]
    public async Task Handle_L1CacheHit_ReturnsCachedDataWithoutCallingRepo()
    {
        // Arrange
        var cachedList = MakeDtos();
        object? outVal = null;
        _localCache.TryGetValue(Arg.Any<object>(), out outVal)
            .Returns(x => { x[1] = (object)cachedList; return true; });

        // Act
        var result = await _handler.Handle(new GetBlogPostsQuery(), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        await _repo.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
        await _distributedCache.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_L2CacheHit_DeserializesAndPopulatesL1()
    {
        // Arrange
        object? outVal = null;
        _localCache.TryGetValue(Arg.Any<object>(), out outVal).Returns(false);

        var dtos = MakeDtos();
        var bytes = JsonSerializer.SerializeToUtf8Bytes(dtos, JsonOpts);
        _distributedCache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<byte[]?>(bytes));

        // Act
        var result = await _handler.Handle(new GetBlogPostsQuery(), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        // IMemoryCache.Set<T> calls CreateEntry — verify L1 was populated
        _localCache.Received(1).CreateEntry(Arg.Any<object>());
        await _repo.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CacheMiss_CallsRepoAndPopulatesBothCaches()
    {
        // Arrange
        object? outVal = null;
        _localCache.TryGetValue(Arg.Any<object>(), out outVal).Returns(false);
        _distributedCache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<byte[]?>(null));

        var post1 = BlogPost.Create("T1", "C1", "A1");
        var post2 = BlogPost.Create("T2", "C2", "A2");
        _repo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<BlogPost> { post1, post2 });

        // Act
        var result = await _handler.Handle(new GetBlogPostsQuery(), CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        // Verify both caches were populated
        _localCache.Received(1).CreateEntry(Arg.Any<object>());
        await _distributedCache.Received(1).SetAsync(
            Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RepoThrows_ReturnsFailResult()
    {
        // Arrange
        object? outVal = null;
        _localCache.TryGetValue(Arg.Any<object>(), out outVal).Returns(false);
        _distributedCache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<byte[]?>(null));
        _repo.GetAllAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("db error"));

        // Act
        var result = await _handler.Handle(new GetBlogPostsQuery(), CancellationToken.None);

        // Assert
        result.Failure.Should().BeTrue();
        result.Error.Should().Contain("db error");
    }
}
