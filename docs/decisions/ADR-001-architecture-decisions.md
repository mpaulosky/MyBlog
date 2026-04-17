# ADR-001: Core Architecture Decisions for MyBlog

**Date:** 2026-04-17  
**Status:** Accepted  
**Deciders:** MyBlog Squad Team

---

## Context

MyBlog is a training/learning project built with .NET 10 and .NET Aspire 9.2.2. It demonstrates a realistic blog CRUD application using modern .NET patterns. The goal is to learn and demonstrate these patterns in a structured way — not to build a production system.

The project evolved from a simple in-memory implementation toward a realistic, production-like stack so learners can see how these technologies compose. The decisions below were finalized through team discussion and rubber-duck review by Aragorn (tech lead) and the MyBlog Squad.

---

## Decisions

### 1. Vertical Slice Architecture (VSA) with MediatR

**What was decided:**  
Organize the application by feature (vertical slices) rather than by technical layer. Each slice owns its command/query, handler, validator, and DTO. MediatR is used as the in-process message bus to decouple request dispatch from handler implementation (CQRS style).

**Why:**
- VSA is the natural evolution from Clean Architecture for CRUD-heavy apps — handlers are self-contained and easy to locate.
- MediatR provides a consistent request/response contract without coupling callers to handler implementations.
- For a training project, VSA makes it easy to add a new feature (e.g., Comments) without touching existing slices.
- MediatR pipeline behaviors (validation, logging, caching) are a powerful learning topic that integrates naturally.

**Alternatives considered:**
- *Clean Architecture with service layer*: More ceremony (Services, DTOs mapped through layers), less intuitive for CRUD operations. Retained Clean Architecture's dependency inversion principle, but without the strict layer separation overhead.
- *Minimal API handlers directly*: Fine for micro-services, but loses the teachable MediatR pipeline pattern.

**Key implementation notes:**
- Each feature folder under `src/Web/Features/<Feature>/` contains `<Feature>Commands.cs`, `<Feature>Queries.cs`, and `<Feature>Handler.cs`.
- `IRequest<T>` / `IRequestHandler<TRequest, TResponse>` from MediatR are the standard contracts.
- Pipeline behaviors are registered in DI: `AddMediatR(cfg => cfg.AddOpenBehavior(typeof(ValidationBehavior<,>)))`.

---

### 2. MongoDB via EF Core Adapter (MongoDB.EntityFrameworkCore)

**What was decided:**  
Use `MongoDB.EntityFrameworkCore` (the official EF Core provider for MongoDB) instead of the raw MongoDB .NET driver. A `BlogDbContext` inheriting `DbContext` owns the `DbSet<BlogPost>` mapping. The context is resolved via `IDbContextFactory<BlogDbContext>` for Blazor Server safety.

**Why:**
- EF Core's familiar DbContext/LINQ API lowers the learning curve — learners already know EF Core patterns.
- The EF Core adapter is the MongoDB-recommended path for teams familiar with relational EF Core workflows.
- `IDbContextFactory` is essential for Blazor Server: the Blazor circuit is long-lived, but DbContext is not thread-safe. The factory pattern creates a scoped context per operation, preventing state leaks across concurrent renders.
- Using EF Core conventions (owned types, value objects) teaches MongoDB document modeling idioms without driver-level ceremony.

**Alternatives considered:**
- *Raw MongoDB .NET driver*: More flexible and performant, but steeper learning curve and more boilerplate for aggregation/query building.
- *Repository pattern wrapping raw driver*: An extra abstraction layer we decided not to add (see Decision 6 regarding `InMemoryBlogPostRepository`).

**Key implementation notes:**
- `BlogDbContext` is registered with `AddDbContextFactory<BlogDbContext>()` in DI.
- Connection strings are provided by Aspire's resource model (`builder.AddMongoDB("mongodb")`).
- Entity configuration uses `OnModelCreating` fluent API: `modelBuilder.Entity<BlogPost>().ToCollection("blog_posts")`.
- No migrations — MongoDB is schema-less; EF Core adapter handles mapping at runtime.

---

### 3. Caching at MediatR Handler Level (Not Repository Decorator)

**What was decided:**  
Caching logic lives inside MediatR handlers (or a dedicated `CachingBehavior<TRequest, TResponse>` pipeline behavior), not in a repository decorator. Cache entries store DTOs, not domain entities.

**Why:**
- Caching at the handler level gives precise control over which queries are cached and with what TTL — each query handler can opt in/out independently.
- A repository decorator would cache at the data-access layer, hiding caching from the application layer and making it harder to reason about cache invalidation boundaries.
- Caching DTOs (not entities) means the cache holds data that is already shaped for the consumer — no risk of cached entities being mutated by the application layer.
- MediatR pipeline behaviors (e.g., `CachingBehavior<,>`) are a clean, teachable pattern for cross-cutting concerns.
- Redis (via Aspire) is the backing cache store, registered as `IDistributedCache`.

**Alternatives considered:**
- *Repository decorator*: Rejected — hides cache semantics from handlers and makes invalidation trickier when handler-level context (e.g., user roles) matters.
- *Output caching middleware*: Too coarse-grained for a Blazor Server app that assembles data from multiple queries per render.
- *In-memory IMemoryCache*: Considered for simplicity, but IDistributedCache (Redis) is more realistic and works across instances.

**Key implementation notes:**
- `CachingBehavior<TRequest, TResponse>` implements `IPipelineBehavior<TRequest, TResponse>` and checks whether `TRequest` implements `ICacheableQuery`.
- Cache keys are derived from the query type name + serialized parameters.
- Cache invalidation happens explicitly in write handlers (Commands call `IDistributedCache.RemoveAsync(key)`).
- DTOs returned by query handlers are the cache payload — entities never leave the handler.

---

### 4. Auth0 for Authentication and Authorization

**What was decided:**  
Use `Auth0.AspNetCore.Authentication` SDK for server-side authentication in the Blazor Server application. Auth0 handles identity, token issuance, and social login. The application trusts Auth0 JWTs and maps claims to ASP.NET Core's identity model.

**Why:**
- Auth0 provides a production-realistic auth experience without building identity infrastructure from scratch — appropriate for a training project that wants to demonstrate real-world auth patterns.
- The `Auth0.AspNetCore.Authentication` SDK integrates cleanly with ASP.NET Core's `IAuthenticationService` and `ClaimsPrincipal` — minimal custom code.
- Auth0's free tier is sufficient for a training project (up to 7,500 monthly active users).
- OAuth2/OIDC flows (Authorization Code + PKCE) are industry-standard learning topics.

**Alternatives considered:**
- *ASP.NET Core Identity (local)*: More control over user storage, but requires a database for users and is significantly more setup (migrations, hashing, etc.). Out of scope for the training focus.
- *Microsoft Entra ID (Azure AD)*: Production-grade, but requires an Azure tenant and more complex app registration. Auth0 has a simpler developer experience for learning.
- *No auth*: Rejected — adding auth is a key learning goal and realistic blog apps require it.

**Key implementation notes:**
- `services.AddAuth0WebAppAuthentication(options => { options.Domain = ...; options.ClientId = ...; })` in `Program.cs`.
- Auth0 tenant configuration (Domain, ClientId, ClientSecret) is stored in `appsettings.json` / user secrets — never committed to source.
- Aspire resource model passes secrets to the Web project via environment variables at runtime.
- `[Authorize]` attribute and `AuthorizeView` component control access at page/component level.
- Claims mapping: `sub` claim → user ID; custom `roles` claim → ASP.NET Core roles.

---

### 5. Role-Based Authorization: Author and Admin

**What was decided:**  
Two application roles are defined:
- **Author** — can Create, Read, Update, and Delete their own blog posts.
- **Admin** — can perform all Author actions plus manage user roles (assign/revoke Author role via Auth0 Management API).

**Why:**
- Role-based authorization (RBAC) is the standard first step in access control — it's teachable and maps directly to real-world requirements.
- A two-role model covers the primary learning objectives (protecting write operations, distinguishing admin vs. regular user) without over-engineering.
- Auth0's role management integrates with the Management API, providing a realistic pattern for role assignment without building a custom admin panel from scratch.

**Alternatives considered:**
- *Policy-based authorization only*: More flexible but harder to understand initially. RBAC serves as the foundation; policies can be layered on top later.
- *Resource-based authorization (ownership checks)*: Considered for "Author can only edit their own posts" — this is implemented as a policy check inside handlers (`if (post.AuthorId != currentUserId) throw UnauthorizedException`) rather than a framework-level resource handler.
- *Claims-based fine-grained permissions*: Overly complex for a training project. Can be introduced as a future extension.

**Key implementation notes:**
- Roles are managed in Auth0 and included in the JWT as a custom claim (`namespace/roles`).
- `Program.cs` maps the custom claim to `ClaimTypes.Role` so standard `[Authorize(Roles = "Author")]` works.
- Handler-level ownership check: `CreatePostCommand` sets `AuthorId` from the current user's `sub` claim; `UpdatePostCommand` verifies `post.AuthorId == currentUserId`.
- Admin role grants access to a user-management page that calls Auth0 Management API to list/assign roles.

---

### 6. IDbContextFactory for Blazor Server (No InMemoryBlogPostRepository)

**What was decided:**  
`IDbContextFactory<BlogDbContext>` is the exclusive data access pattern. The `InMemoryBlogPostRepository` and `IBlogPostRepository` interface have been removed from the production code path. Only the MongoDB-backed `BlogDbContext` is used at runtime.

**Why:**
- Blazor Server circuits are long-lived and can service multiple concurrent render events. A singleton or scoped `DbContext` would be shared across these events, causing `DbContext` thread-safety violations.
- `IDbContextFactory` creates a fresh `DbContext` instance per database operation (handlers call `await _factory.CreateDbContextAsync()`), which is the officially recommended pattern for Blazor Server.
- Removing `InMemoryBlogPostRepository` eliminates dead code and a false architectural signal — learners should see the real MongoDB path, not an in-memory shim.
- Unit tests that previously relied on `InMemoryBlogPostRepository` are replaced with NSubstitute mocks of `IDbContextFactory` or integration tests using a real MongoDB (via Aspire test host or Testcontainers).

**Alternatives considered:**
- *Keep InMemoryBlogPostRepository for tests*: Rejected — it introduced a divergent code path and made tests less realistic. NSubstitute and Testcontainers provide better test isolation without the maintenance burden.
- *Scoped DbContext (without factory)*: Works in API/MVC scenarios but not safe for Blazor Server. The factory pattern is the correct choice here.
- *Unit of Work pattern*: Considered as an abstraction over `DbContext`, but EF Core's `DbContext` itself implements the Unit of Work pattern — adding another wrapper is unnecessary indirection for a training project.

**Key implementation notes:**
- DI registration: `builder.Services.AddDbContextFactory<BlogDbContext>(options => options.UseMongoDB(connectionString, "myblog"))`.
- Handlers inject `IDbContextFactory<BlogDbContext>` and call `await using var context = await _factory.CreateDbContextAsync(cancellationToken)`.
- `await using` ensures `Dispose` is called after each operation, returning the connection to the pool.
- Integration tests use `WebApplicationFactory` + Aspire test host with a real MongoDB container.

---

## Consequences

### Positive

- **Cohesive learning path**: Each decision reinforces the others — VSA + MediatR pipeline behaviors + caching + auth all compose naturally.
- **Production-realistic**: Learners see patterns they will encounter in real .NET projects (Auth0, MongoDB, Redis, IDbContextFactory).
- **Testable by design**: MediatR decouples handlers from callers; IDbContextFactory makes database operations injectable and mockable.
- **No dead code**: Removing `InMemoryBlogPostRepository` keeps the codebase honest — what you see is what runs.

### Negative / Trade-offs

- **Increased setup complexity**: Learners need Docker (for MongoDB + Redis via Aspire), an Auth0 tenant, and user secrets configured before the app runs end-to-end.
- **MediatR overhead**: Adds indirection for simple CRUD operations. In a tiny training app the ceremony can feel excessive — the trade-off is intentional pedagogy.
- **MongoDB.EntityFrameworkCore maturity**: The EF Core adapter for MongoDB is newer than the raw driver and has some feature gaps (e.g., limited LINQ translation). Learners may hit edge cases.
- **Auth0 dependency**: Requires internet connectivity and an external service. Offline development requires mocking the auth layer.

### Neutral

- The project remains explicitly a **training project** — architectural purity is occasionally traded for teachability.
- Future extensions (Comments, Tags, Search) can be added as new vertical slices without changing existing code.
