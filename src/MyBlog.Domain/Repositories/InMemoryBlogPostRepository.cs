using System.Collections.Concurrent;
using MyBlog.Domain.Entities;
using MyBlog.Domain.Interfaces;

namespace MyBlog.Domain.Repositories;

public sealed class InMemoryBlogPostRepository : IBlogPostRepository
{
    private readonly ConcurrentDictionary<Guid, BlogPost> _store = new();

    public Task<BlogPost?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_store.TryGetValue(id, out var post) ? post : null);

    public Task<IReadOnlyList<BlogPost>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<BlogPost>>(_store.Values.ToList());

    public Task AddAsync(BlogPost post, CancellationToken ct = default)
    {
        _store[post.Id] = post;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(BlogPost post, CancellationToken ct = default)
    {
        _store[post.Id] = post;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
