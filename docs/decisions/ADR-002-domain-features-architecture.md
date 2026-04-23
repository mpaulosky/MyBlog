---
post_title: ADR-002 — Domain/Features Layer Architecture
author1: Matthew Paulosky
post_slug: adr-002-domain-features-architecture
microsoft_alias: mpaulosky
featured_image: https://myblog.local/images/architecture.jpg
categories: [Architecture]
tags: [CQRS, MediatR, VSA, LayerIsolation]
ai_note: false
summary: Analysis of dual-layer CQRS handler structure (Domain vs Web Features) and the rationale for a two-layer pattern that maintains clean architecture principles while enabling caching and error handling at the application layer.
post_date: 2026-04-23
---

# ADR-002 — Domain/Features Layer Architecture

## Status

**Accepted**

## Decision

Maintain the current two-layer CQRS handler pattern:

- **Domain/Features**: Pure domain business logic (entity operations, no infrastructure concerns)
- **Web/Features**: Application orchestration (caching, error handling, DTOs)

Both layers process the same commands/queries but serve complementary purposes. This is **NOT a violation of VSA** because:

1. **Clean layer separation** — Domain has zero Web/infrastructure dependencies
2. **Complementary responsibilities** — Domain = business logic, Web = orchestration + caching
3. **Legitimate CQRS pattern** — Two-layer command/query handlers are a known pattern for separating domain logic from application infrastructure
4. **Testability** — Domain handlers can be tested without infrastructure; Web handlers test caching/error handling

## Problem

During test reorganization ([#80](https://github.com/MyBlog/issues/80)), we discovered:

```
src/Domain/Features/BlogPosts/Commands/CreateBlogPost/
  ├── CreateBlogPostCommand.cs          (Domain)
  ├── CreateBlogPostCommandHandler.cs   (Domain)
  └── CreateBlogPostCommandValidator.cs (Domain)

src/Web/Features/BlogPosts/Create/
  ├── CreateBlogPostCommand.cs          (Web)
  ├── CreateBlogPostCommandValidator.cs (Web)
  ├── CreateBlogPostHandler.cs          (Web)
  └── Create.razor
```

**Initial Concern**: Are we violating Vertical Slice Architecture (VSA) by having identical feature folders in two layers?

## Analysis

### Current Pattern: Two-Layer CQRS

**Domain/Features/BlogPosts/Commands/CreateBlogPost/CreateBlogPostCommandHandler:**

```csharp
public sealed class CreateBlogPostCommandHandler : IRequestHandler<CreateBlogPostCommand, Result<Guid>>
{
    private readonly IBlogPostRepository _repository;

    public CreateBlogPostCommandHandler(IBlogPostRepository repository) => _repository = repository;

    public async Task<Result<Guid>> Handle(CreateBlogPostCommand request, CancellationToken cancellationToken)
    {
        var post = BlogPost.Create(request.Title, request.Content, request.Author);
        await _repository.AddAsync(post, cancellationToken);
        return Result.Ok(post.Id);
    }
}
```

**Characteristics:**
- Pure domain logic: entity creation, repository persistence
- No infrastructure concerns (caching, error handling beyond domain validation)
- No dependencies on Web/Blazor
- Reusable: could be called from REST API, background job, etc.

---

**Web/Features/BlogPosts/Create/CreateBlogPostHandler:**

```csharp
public sealed class CreateBlogPostHandler(
    IBlogPostRepository repo,
    IMemoryCache localCache,
    IDistributedCache distributedCache) : IRequestHandler<CreateBlogPostCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateBlogPostCommand request, CancellationToken ct)
    {
        try
        {
            var post = BlogPost.Create(request.Title, request.Content, request.Author);
            await repo.AddAsync(post, ct);
            localCache.Remove("blog:all");          // Invalidate query cache
            _ = distributedCache.RemoveAsync("blog:all", ct);
            return Result.Ok<Guid>(post.Id);
        }
        catch (Exception ex)
        {
            return Result.Fail<Guid>(ex.Message);
        }
    }
}
```

**Characteristics:**
- Application orchestration: validates, creates entity, persists, **invalidates cache**
- Infrastructure concerns: caching, error handling
- Depends on IMemoryCache and IDistributedCache (Web concerns)
- Stateless component for Blazor Server use

---

**Query Side (Similar Pattern):**

**Domain/Features/BlogPosts/Queries/GetAllBlogPosts/GetAllBlogPostsQueryHandler:**
```csharp
public sealed class GetAllBlogPostsQueryHandler : IRequestHandler<GetAllBlogPostsQuery, Result<IReadOnlyList<BlogPost>>>
{
    public async Task<Result<IReadOnlyList<BlogPost>>> Handle(GetAllBlogPostsQuery request, CancellationToken ct)
    {
        var posts = await _repository.GetAllAsync(ct);
        return Result.Ok(posts);  // Returns domain entities
    }
}
```

**Web/Features/BlogPosts/List/GetBlogPostsHandler:**
```csharp
public sealed class GetBlogPostsHandler(
    IBlogPostRepository repo,
    IMemoryCache localCache,
    IDistributedCache distributedCache) : IRequestHandler<GetBlogPostsQuery, Result<IReadOnlyList<BlogPostDto>>>
{
    public async Task<Result<IReadOnlyList<BlogPostDto>>> Handle(GetBlogPostsQuery request, CancellationToken ct)
    {
        // Check L1 (local) cache
        if (localCache.TryGetValue("blog:all", out List<BlogPostDto>? cached) && cached is not null)
            return Result.Ok<IReadOnlyList<BlogPostDto>>(cached);

        // Check L2 (Redis) cache
        var bytes = await distributedCache.GetAsync("blog:all", ct);
        if (bytes is not null)
        {
            var fromRedis = JsonSerializer.Deserialize<List<BlogPostDto>>(bytes, JsonOpts)!;
            localCache.Set("blog:all", fromRedis, LocalOpts);
            return Result.Ok<IReadOnlyList<BlogPostDto>>(fromRedis);
        }

        // Cache miss: hit DB, populate both caches, return DTOs
        var posts = await repo.GetAllAsync(ct);
        var dtos = posts.Select(p => p.ToDto()).ToList();
        localCache.Set("blog:all", dtos, LocalOpts);
        await distributedCache.SetAsync("blog:all", JsonSerializer.SerializeToUtf8Bytes(dtos, JsonOpts), RedisOpts, ct);
        return Result.Ok<IReadOnlyList<BlogPostDto>>(dtos);
    }
}
```

### Key Findings

| Aspect | Domain Handlers | Web Handlers |
|--------|-----------------|-------------|
| **Responsibility** | Domain business logic | Application orchestration |
| **Returns** | Domain entities (`BlogPost`) | DTOs (`BlogPostDto`) |
| **Caching** | None | L1 (IMemoryCache) + L2 (Redis) |
| **Error Handling** | Domain validation only | Infrastructure + domain validation |
| **Dependencies** | `IBlogPostRepository` only | `IBlogPostRepository`, `IMemoryCache`, `IDistributedCache` |
| **Web/Aspire Coupling** | None | Yes (cache, DTOs) |
| **Use Case** | Unit testing, background jobs | Blazor Server UI handlers |

### Layering Rule Validation

✅ **Domain → Web dependency rule: CLEAN**
- Domain project has **zero** references to Web
- Architecture tests confirm: `VsaLayerTests.Data_Layer_Should_Not_Be_Referenced_Outside_Web()` passes

✅ **No circular dependencies**
- Web depends on Domain only
- Program.cs registers both: `cfg.RegisterServicesFromAssembly(typeof(Program).Assembly); cfg.RegisterServicesFromAssembly(typeof(BlogPost).Assembly);`

✅ **MediatR registration: INTENTIONAL**
- Both assemblies scanned deliberately for dual-layer pattern
- No name conflicts: `CreateBlogPostCommandHandler` (Domain) vs `CreateBlogPostHandler` (Web) — different types

✅ **Clean architecture principles maintained**
- Entities/repositories in Domain (outer ring)
- Infrastructure concerns (caching, DTOs) in Web (inner ring)

## Why This Pattern?

### 1. **Separation of Concerns**

- Domain handlers are **testable without infrastructure** (mock repository)
- Web handlers handle **application-specific concerns** (caching, Blazor state)
- Can test domain logic in isolation; test caching separately

### 2. **Infrastructure-Agnostic Domain Logic**

Future use cases (REST API, GraphQL, background job) can:
- Use Domain handlers directly (pure business logic)
- Or wrap them with their own Web/Application handlers for infrastructure concerns

### 3. **Cache Invalidation Strategy**

Write operations (Create, Update, Delete) must invalidate caches. This is an **application-layer concern**, not domain logic. Domain handlers don't know about caches; Web handlers do.

### 4. **DTO Mapping**

DTOs are generated at the Web layer because:
- Blazor components receive `BlogPostDto` (not domain `BlogPost` entity)
- Separates UI concerns from domain model
- Domain queries return entities (for reusability); Web queries return DTOs (for Blazor)

### 5. **Error Handling Philosophy**

- Domain: Validation errors are `Result.Fail()` with domain-specific error codes
- Web: Infrastructure errors (concurrency, cache failures) handled at application layer

Example: `EditBlogPostHandler` catches `DbUpdateConcurrencyException` and returns a specific error code for UI handling.

## Alternatives Considered

### Option A: Single-Layer (Domain Only)

```csharp
// All logic in Domain/Features
public sealed class CreateBlogPostCommandHandler : IRequestHandler<CreateBlogPostCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateBlogPostCommand request, CancellationToken ct)
    {
        var post = BlogPost.Create(request.Title, request.Content, request.Author);
        await repo.AddAsync(post, ct);
        _localCache.Remove("blog:all");  // ❌ Domain knows about Web caches
        return Result.Ok(post.Id);
    }
}
```

**Rejected Because:**
- Couples domain to infrastructure (IMemoryCache, IDistributedCache)
- Violates DDD: domain shouldn't know about caching layers
- Not reusable for non-Web use cases

### Option B: Single-Layer (Web Only)

```csharp
// Move all Domain/Features handlers to Web/Features
// Domain contains only entities and interfaces
```

**Rejected Because:**
- Reduces testability: can't test domain logic without Web dependencies
- Mixes domain business logic with application orchestration
- Harder to reason about responsibilities

### Option C: Current Pattern (Two-Layer) ✅

**Chosen Because:**
- Clean separation of concerns
- Domain logic testable in isolation
- Infrastructure concerns isolated at Web layer
- Extensible: future layers (API, GraphQL) can reuse Domain handlers

## Implementation Notes

### Registration in Program.cs

Both Domain and Web assemblies are intentionally scanned:

```csharp
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);        // Web
    cfg.RegisterServicesFromAssembly(typeof(BlogPost).Assembly);       // Domain
});
```

**Behavior:** When `Sender.Send(new CreateBlogPostCommand(...))` is called, **Web handler is used** because both register for `IRequestHandler<CreateBlogPostCommand, Result<Guid>>`, and Web wins the registration order.

**This is intentional:** Web handlers are the entry point from Blazor components. Domain handlers exist for testing and future extensibility.

### Future Refactoring (If Needed)

If Web handlers become bloated, consider a **three-layer pattern:**

```
Domain/Features       → Pure business logic
Application/Features  → DTOs, validators, error handling
Web/Features         → Blazor components only
```

But with current scope, two layers are sufficient.

## Testing Implications

### Unit Tests for Domain Handlers

```csharp
[Fact]
public async Task CreateBlogPostCommandHandler_Should_Create_And_Return_Id()
{
    var mockRepo = Substitute.For<IBlogPostRepository>();
    var handler = new CreateBlogPostCommandHandler(mockRepo);
    
    var result = await handler.Handle(
        new CreateBlogPostCommand("Title", "Content", "Author"), 
        CancellationToken.None);
    
    result.Success.Should().BeTrue();
    await mockRepo.Received(1).AddAsync(Arg.Any<BlogPost>(), Arg.Any<CancellationToken>());
}
```

Domain tests mock the repository. No infrastructure needed.

### Unit Tests for Web Handlers

```csharp
[Fact]
public async Task GetBlogPostsHandler_Should_Populate_Cache()
{
    var mockRepo = Substitute.For<IBlogPostRepository>();
    var mockLocal = Substitute.For<IMemoryCache>();
    var mockRedis = Substitute.For<IDistributedCache>();
    var handler = new GetBlogPostsHandler(mockRepo, mockLocal, mockRedis);
    
    // Simulate cache miss
    mockLocal.TryGetValue("blog:all", out Arg.Any<List<BlogPostDto>>()).Returns(false);
    
    var result = await handler.Handle(new GetBlogPostsQuery(), CancellationToken.None);
    
    result.Success.Should().BeTrue();
    mockLocal.Received(1).Set("blog:all", Arg.Any<List<BlogPostDto>>(), Arg.Any<MemoryCacheEntryOptions>());
}
```

Web tests mock caches. No database needed.

## Decisions & Constraints

| Aspect | Decision |
|--------|----------|
| **Dual handler pattern** | Keep (provides separation of concerns) |
| **Domain handler naming** | `*CommandHandler`, `*QueryHandler` (DDD convention) |
| **Web handler naming** | `*Handler` (simpler, distinct from Domain) |
| **DTO generation** | Web layer only (Blazor concern) |
| **Cache invalidation** | Web handlers only (infrastructure concern) |
| **Error handling** | Domain: validation; Web: infrastructure + validation |
| **Future REST API** | Reuse Domain handlers, wrap with API-specific orchestration |

## Conclusion

The current two-layer CQRS handler pattern is **architecturally sound** and **not a VSA violation**. It maintains clean dependency rules while enabling:

1. Testable domain logic in isolation
2. Infrastructure-agnostic business logic
3. Extensibility to new frontends (REST API, GraphQL)
4. Clear separation of domain responsibility from application orchestration

**This pattern should be documented in ARCHITECTURE.md** for future contributors to understand the rationale.

---

## References

- [Domain-Driven Design - Eric Evans](https://www.domainlanguage.com/ddd/)
- [Vertical Slice Architecture - Jimmy Bogard](https://jimmybogard.com/vertical-slice-architecture/)
- [CQRS Pattern - Microsoft Docs](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [MediatR GitHub](https://github.com/jbogard/MediatR)

---

**Recorded by:** Sam (Backend Developer)  
**Date:** 2026-04-23  
**Related Issues:** #97 (Domain/Features Architecture Investigation), #58 (CQRS + MediatR Domain layer handlers)
