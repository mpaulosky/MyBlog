---
post_title: "ADR: Extract IBlogPostCacheService Abstraction"
author1: "Frodo"
post_slug: "adr-sprint5-caching-abstraction"
microsoft_alias: ""
featured_image: ""
categories: ["Architecture"]
tags: ["caching", "redis", "abstraction", "adr"]
ai_note: "AI-assisted"
summary: "Decision to extract a two-tier cache service abstraction to eliminate duplication across BlogPost handlers."
post_date: "2026-04-23"
---

## Context

Sprint 5 introduced a two-tier caching strategy across all four BlogPost MediatR handlers (`GetBlogPostsHandler`, `EditBlogPostHandler`, `CreateBlogPostHandler`, `DeleteBlogPostHandler`). Each handler independently declares:

- `MemoryCacheEntryOptions` (L1 in-memory, 1-minute TTL)
- `DistributedCacheEntryOptions` (L2 Redis, 5-minute TTL)
- `JsonSerializerOptions` for serialization to/from Redis bytes
- Inline cache key strings (`"blog:all"`, `$"blog:{id}"`)

This results in four copies of the same boilerplate. Cache key strings are magic literals scattered across files, making a future key rename a multi-file search-and-replace. The `JsonSerializerOptions` instance is duplicated rather than shared.

Any change to TTL policy, serialization options, or key naming requires touching every handler — a violation of the DRY principle and a maintenance hazard.

---

## Decision

Extract a dedicated two-tier cache service behind the interface `IBlogPostCacheService`, implemented by `BlogPostCacheService`. Cache key constants are centralised in a companion static class `BlogPostCacheKeys`.

### New types

| Type | Kind | Responsibility |
|------|------|----------------|
| `IBlogPostCacheService` | Interface | Contract for L1 + L2 blog post cache operations |
| `BlogPostCacheService` | Class | Implementation wrapping `IMemoryCache` + `IDistributedCache` |
| `BlogPostCacheKeys` | Static class | Centralized cache key constants (`All`, `ById(Guid)`) |

### Handler refactor

All four handlers replace their inline `IMemoryCache` + `IDistributedCache` parameters with a single `IBlogPostCacheService` injection. The TTL constants, serialization options, and cache key logic move into `BlogPostCacheService`.

### Registration

`BlogPostCacheService` is registered in `Program.cs` as a scoped or singleton service alongside `IMemoryCache` and the Redis `IDistributedCache`.

---

## Status

Accepted

---

## Consequences

### Positive

- **Eliminates duplication** — cache logic lives in one place; all four handlers become simpler.
- **Single TTL policy** — changing L1 or L2 TTL requires editing one file.
- **Centralised key management** — `BlogPostCacheKeys` prevents typos and enables compile-time refactoring.
- **Testable abstraction** — handlers under test can receive a mock `IBlogPostCacheService` instead of coordinating two separate mock caches.
- **Consistent serialization** — `JsonSerializerOptions` is created once inside `BlogPostCacheService`, ensuring uniform behaviour.

### Negative

- **Extra abstraction layer** — introduces an interface + implementation pair for what is conceptually simple cache I/O. Teams unfamiliar with the abstraction need to trace one more indirection when debugging cache behaviour.
- **Discoverability** — a developer reading a handler for the first time will not see caching details inline; they must navigate to `BlogPostCacheService`. Mitigation: XML doc comments on the interface methods document what each call does.
- **Migration cost** — all four existing handlers require a signature change and removal of their inline cache fields. The change is mechanical but touches multiple files.
