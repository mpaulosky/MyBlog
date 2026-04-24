# Sprint 5 — XML Doc Comment Stubs

XML doc comment stubs for Sam to apply to the new cache abstraction types introduced in Sprint 5.

---

## `IBlogPostCacheService`

```csharp
/// <summary>
/// Provides two-tier (L1 in-memory + L2 Redis) cache operations for blog post data.
/// </summary>
public interface IBlogPostCacheService
{
    /// <summary>
    /// Retrieves all blog posts from the cache, checking L1 (in-memory) before L2 (Redis).
    /// Returns <see langword="null"/> when no cached entry exists.
    /// </summary>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that resolves to a read-only list of <see cref="BlogPostDto"/>
    /// instances, or <see langword="null"/> if the entry is not present in either cache tier.
    /// </returns>
    Task<IReadOnlyList<BlogPostDto>?> GetAllAsync(CancellationToken ct);

    /// <summary>
    /// Stores all blog posts in both cache tiers (L1 in-memory and L2 Redis).
    /// </summary>
    /// <param name="posts">The list of blog post DTOs to cache.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous set operation.</returns>
    Task SetAllAsync(IReadOnlyList<BlogPostDto> posts, CancellationToken ct);

    /// <summary>
    /// Retrieves a single blog post by its unique identifier from the cache,
    /// checking L1 (in-memory) before L2 (Redis).
    /// Returns <see langword="null"/> when no cached entry exists for the given ID.
    /// </summary>
    /// <param name="id">The unique identifier of the blog post to retrieve.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> that resolves to the cached <see cref="BlogPostDto"/>,
    /// or <see langword="null"/> if no entry is found.
    /// </returns>
    Task<BlogPostDto?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Stores a single blog post in both cache tiers (L1 in-memory and L2 Redis),
    /// keyed by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier used as the cache key.</param>
    /// <param name="post">The blog post DTO to cache.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous set operation.</returns>
    Task SetByIdAsync(Guid id, BlogPostDto post, CancellationToken ct);

    /// <summary>
    /// Removes the all-posts cache entry from both L1 (in-memory) and L2 (Redis).
    /// Call this after any write operation that affects the full post list
    /// (create, edit, delete, publish/unpublish).
    /// </summary>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous invalidation operation.</returns>
    Task InvalidateAllAsync(CancellationToken ct);

    /// <summary>
    /// Removes the per-post cache entry for the given identifier from both
    /// L1 (in-memory) and L2 (Redis).
    /// Call this after any write operation that modifies or deletes a specific post.
    /// </summary>
    /// <param name="id">The unique identifier of the blog post whose cache entry should be removed.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous invalidation operation.</returns>
    Task InvalidateByIdAsync(Guid id, CancellationToken ct);
}
```

---

## `BlogPostCacheKeys`

```csharp
/// <summary>Cache key constants for blog post entries.</summary>
public static class BlogPostCacheKeys
{
    /// <summary>Cache key for the full list of blog posts.</summary>
    public const string All = "blog:all";

    /// <summary>
    /// Returns the cache key for a single blog post identified by <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The unique identifier of the blog post.</param>
    /// <returns>A string cache key in the format <c>blog:{id}</c>.</returns>
    public static string ById(Guid id) => $"blog:{id}";
}
```

---

## `BlogPostCacheService`

```csharp
/// <summary>
/// Two-tier cache implementation for blog post data, combining an L1 in-process
/// <see cref="IMemoryCache"/> (1-minute TTL) with an L2 distributed
/// <see cref="IDistributedCache"/> backed by Redis (5-minute TTL).
/// </summary>
/// <remarks>
/// Read operations check L1 first; on an L1 miss they fall through to L2 and
/// back-fill L1 on a hit. Write operations populate both tiers simultaneously.
/// Invalidation removes from both tiers.
/// </remarks>
public sealed class BlogPostCacheService : IBlogPostCacheService
```

---

## README.md — Needed Updates

The current `README.md` does **not** mention caching or Redis. The following two sections require updates once Sprint 5 is merged:

### Technology Stack section

Add entries for the caching layer:

```markdown
- **IMemoryCache** — L1 in-process cache (1-minute TTL per entry)
- **Redis via .NET Aspire** — L2 distributed cache (5-minute TTL); provisioned by
  `builder.AddRedis("redis")` in `AppHost`
```

Also update the stale line:

```markdown
- **In-Memory Repository** — No database (training project by design)
```

This is no longer accurate after the MongoDB migration (Sprint 4). It should read:

```markdown
- **MongoDB via EF Core Adapter** — Document store for blog posts,
  accessed through `IDbContextFactory<BlogDbContext>`
```

### Features section

No new user-visible features are introduced by the caching abstraction. No changes needed here.

### Learning Objectives section

Consider adding a seventh objective once Sprint 5 ships:

```markdown
7. **Two-Tier Caching** — L1 IMemoryCache + L2 Redis via IBlogPostCacheService abstraction,
   cache invalidation on write, DRY cache key management with BlogPostCacheKeys
```

---

*Stubs prepared by Frodo (Tech Writer) — Sprint 5, 2026-04-23*
