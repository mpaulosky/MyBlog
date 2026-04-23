# MyBlog — Architecture

## Overview

MyBlog is a training blog application built with .NET 10 and .NET Aspire orchestration. It uses Blazor Server rendering, MongoDB for persistence (via the EF Core adapter), Redis for distributed caching, Auth0 for authentication, and MediatR with Vertical Slice Architecture for request handling.

This document describes the solution structure, layer dependencies, and key design decisions that make MyBlog an effective learning tool.

> **Architecture Decisions**: Detailed rationale for each major design choice is captured in [`docs/decisions/`](decisions/index.md). Start with [ADR-001](decisions/ADR-001-architecture-decisions.md) for the full story.

## Tech Stack

| Concern | Technology |
|---------|-----------|
| Runtime | .NET 10 |
| Orchestration | .NET Aspire 9.2.2 |
| UI | Blazor Server (Interactive Server Rendering) |
| Architecture | Vertical Slice Architecture (VSA) + MediatR (CQRS) |
| Database | MongoDB via `MongoDB.EntityFrameworkCore` (`IDbContextFactory`) |
| Caching | Redis via `IDistributedCache` (caching in MediatR handlers) |
| Authentication | Auth0 (`Auth0.AspNetCore.Authentication` SDK) |
| Authorization | Role-based: `Author` (CRUD posts) · `Admin` (CRUD + manage roles) |
| Testing | xUnit · FluentAssertions · NSubstitute · NetArchTest.Rules |

## Solution Structure

```
MyBlog/
├── src/
│   ├── AppHost/              # .NET Aspire orchestration entry point (wires MongoDB, Redis, Web)
│   ├── Domain/               # Core domain layer: BlogPost entity, value objects, domain handlers
│   │   └── Features/
│   │       └── BlogPosts/    # Domain logic: pure CQRS handlers (no infrastructure)
│   ├── ServiceDefaults/      # Aspire shared configuration (OpenTelemetry, health checks)
│   └── Web/                  # Blazor Server app — VSA feature slices, MediatR handlers, Auth0
│       └── Features/
│           └── BlogPosts/    # Web orchestration: caching, DTOs, UI handlers
├── tests/
│   ├── Unit.Tests/           # BlogPost entity tests, handler unit tests (NSubstitute mocks)
│   ├── Architecture.Tests/    # NetArchTest.Rules layer dependency enforcement
│   └── Integration.Tests/     # Aspire test host + MongoDB container integration tests
├── docs/
│   ├── decisions/            # Architecture Decision Records (ADRs)
│   │   ├── index.md          # ADR index
│   │   ├── ADR-001-architecture-decisions.md
│   │   └── ADR-002-domain-features-architecture.md
│   └── ...                   # ARCHITECTURE.md, CONTRIBUTING.md, etc.
├── Directory.Build.props     # Centralized build configuration
├── global.json               # .NET SDK version lock
├── GitVersion.yml            # Semantic versioning configuration
└── MyBlog.slnx               # Solution file
```

### Project Descriptions

#### AppHost

- **Purpose**: .NET Aspire orchestration entry point
- **Responsibility**: Compose and wire together the Domain and Web services
- **Contains**: Service resource definitions, dependency injection, Aspire builder configuration
- **Runs**: The Aspire dashboard for local visualization and health monitoring

#### Domain

- **Purpose**: Core business logic and domain model
- **Responsibility**: BlogPost entity, value objects, domain-layer CQRS handlers
- **Dependencies**: None on other projects
- **Key Types**:
  - `BlogPost` — Domain entity with factory method `Create(title, content, author)` and mutation methods `Update()`, `Publish()`, `Unpublish()`
  - `CreateBlogPostCommandHandler`, `UpdateBlogPostCommandHandler`, etc. — Pure domain logic handlers (no infrastructure concerns)
- **Note**: Domain handlers focus on business logic; Web handlers add orchestration (caching, DTOs). See [ADR-002](decisions/ADR-002-domain-features-architecture.md) for rationale.

#### ServiceDefaults

- **Purpose**: Shared Aspire configuration for cross-cutting concerns
- **Responsibility**: OpenTelemetry setup, health checks, common middleware
- **Dependencies**: None on business code
- **Used by**: AppHost and Web projects

#### Web

- **Purpose**: Blazor Server user interface + application logic via MediatR
- **Responsibility**: Feature slices (VSA), Blazor components/pages, Auth0 integration, caching, DTOs
- **Dependencies**: Domain; resolves MongoDB + Redis via Aspire
- **Structure**:
  ```
  Web/
  ├── Features/
  │   └── BlogPosts/
  │       ├── Create/, Delete/, Edit/, List/  # Web handlers with caching & DTOs
  │       └── Components (Create.razor, etc.)
  ├── Components/
  │   ├── Layout/
  │   │   ├── MainLayout.razor       # Root layout wrapper
  │   │   ├── NavMenu.razor          # Navigation menu
  │   │   └── ReconnectModal.razor   # Aspire reconnection UI
  │   ├── Pages/
  │   │   ├── BlogPosts/
  │   │   │   ├── Index.razor        # List all posts (sends GetBlogPostsQuery via MediatR)
  │   │   │   ├── Create.razor       # New post form (sends CreateBlogPostCommand)
  │   │   │   └── Edit.razor         # Edit post form (sends EditBlogPostCommand)
  │   │   ├── Home.razor             # Landing page
  │   │   ├── Error.razor            # Error handler
  │   │   └── NotFound.razor         # 404 handler
  │   ├── Shared/
  │   │   └── ConfirmDeleteDialog.razor  # Reusable delete confirmation
  │   └── App.razor                  # Root Blazor component
  └── Program.cs                     # DI: MediatR (scans both Domain & Web), DbContextFactory, Auth0, Redis
  ```
- **Handler Pattern**: Two-layer CQRS
  - **Domain handlers** (pure logic): `CreateBlogPostCommandHandler`, `GetAllBlogPostsQueryHandler`
  - **Web handlers** (orchestration): `CreateBlogPostHandler`, `GetBlogPostsHandler` (with caching & DTOs)
  - See [ADR-002](decisions/ADR-002-domain-features-architecture.md) for architectural rationale

#### Test Projects

**Unit.Tests/**
- BlogPost entity tests; MediatR handler unit tests using NSubstitute mocks of `IDbContextFactory`
- Framework: xUnit · FluentAssertions · NSubstitute

**Architecture.Tests/**
- Enforces layer dependency rules (Domain has no Web reference)
- Framework: NetArchTest.Rules

**Integration.Tests/**
- Aspire test host + MongoDB Testcontainer for realistic end-to-end handler tests
- Validates MediatR pipelines, caching behavior, and Auth0 claim mapping

## Layer Diagram

```
┌────────────────────────────────────────────────────────┐
│         Web (Blazor UI + VSA Feature Slices)           │
│   Pages → MediatR → Handlers → BlogDbContext (EF Core) │
│         ↑ Auth0 (OIDC)    ↑ Redis (IDistributedCache)  │
├────────────────────────────────────────────────────────┤
│              Domain (BlogPost Entity)                  │
├────────────────────────────────────────────────────────┤
│   AppHost orchestrates Web + MongoDB + Redis           │
│   ServiceDefaults: OpenTelemetry, Health Checks        │
└────────────────────────────────────────────────────────┘
```

**Dependency Rule**: Web depends on Domain; Domain has no external dependencies. MongoDB and Redis are infrastructure concerns resolved via Aspire at the AppHost level and injected into Web via connection strings.

## Domain Layer

### BlogPost Entity

```csharp
public class BlogPost
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Content { get; private set; }
    public string Author { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsPublished { get; private set; }

    // Factory method
    public static BlogPost Create(string title, string content, string author);

    // Mutable methods
    public void Update(string title, string content);
    public void Publish();
    public void Unpublish();
}
```

**Design Notes**:
- Private setters enforce immutability outside factory/methods
- `CreatedAt` is set once; `UpdatedAt` tracks mutations
- `IsPublished` defaults to `false` (drafts by default)
- No validation logic in the entity (kept simple for training)

### Data Access — BlogDbContext (MongoDB via EF Core)

Data access is handled through `IDbContextFactory<BlogDbContext>`. Each MediatR handler creates a short-lived context per operation:

```csharp
await using var context = await _factory.CreateDbContextAsync(cancellationToken);
var post = await context.BlogPosts.FindAsync(id, cancellationToken);
```

`BlogDbContext` maps the `BlogPost` entity to a MongoDB collection:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<BlogPost>().ToCollection("blog_posts");
}
```

The factory pattern is required for Blazor Server because `DbContext` is not thread-safe and must not be shared across concurrent render events on the same circuit. See [ADR-001 §6](decisions/ADR-001-architecture-decisions.md) for full rationale.

### Two-Layer CQRS Handler Pattern

MyBlog implements a **two-layer CQRS pattern** where both Domain and Web projects contain MediatR handlers for the same commands/queries. This is **not a violation of VSA** but rather a clean separation of concerns:

#### Domain Handlers (Pure Business Logic)

Located in `Domain/Features/BlogPosts/Commands/` and `Domain/Features/BlogPosts/Queries/`:

- **Responsibility**: Domain business logic only
- **Dependencies**: `IBlogPostRepository` only
- **Returns**: Domain entities (`BlogPost`, `IReadOnlyList<BlogPost>`)
- **No Infrastructure**: Zero knowledge of caching, DTOs, or Web concerns
- **Testable**: Can be unit tested with mock repositories
- **Reusable**: Could be called from REST API, GraphQL, or background jobs

Example:
```csharp
public sealed class CreateBlogPostCommandHandler : IRequestHandler<CreateBlogPostCommand, Result<Guid>>
{
    private readonly IBlogPostRepository _repository;

    public async Task<Result<Guid>> Handle(CreateBlogPostCommand request, CancellationToken cancellationToken)
    {
        var post = BlogPost.Create(request.Title, request.Content, request.Author);
        await _repository.AddAsync(post, cancellationToken);
        return Result.Ok(post.Id);
    }
}
```

#### Web Handlers (Application Orchestration)

Located in `Web/Features/BlogPosts/Create/`, `/Edit/`, `/List/`, etc.:

- **Responsibility**: Application-level orchestration for Blazor Server
- **Infrastructure**: Caching (IMemoryCache, IDistributedCache), error handling, DTOs
- **Returns**: DTOs (`BlogPostDto`) for Blazor components
- **Cache Invalidation**: Write handlers explicitly clear caches
- **Testing**: Tested separately to validate caching and error handling

Example:
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
            localCache.Remove("blog:all");                    // Invalidate query cache
            _ = distributedCache.RemoveAsync("blog:all", ct);
            return Result.Ok(post.Id);
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }
    }
}
```

#### Why Two Layers?

1. **Separation of Concerns**: Domain logic isolated from infrastructure concerns
2. **Testability**: Domain logic testable without caches or Blazor dependencies
3. **Extensibility**: Future frontends (REST API, GraphQL) can reuse Domain handlers
4. **Cache Invalidation**: Application layer owns cache lifecycle (not domain responsibility)
5. **DTOs**: Web layer transforms domain entities to DTOs for UI contracts

**Both layers are intentionally registered in Program.cs:**

```csharp
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);  // Web handlers
    cfg.RegisterServicesFromAssembly(typeof(BlogPost).Assembly); // Domain handlers
});
```

When Blazor components dispatch `new CreateBlogPostCommand(...)`, the **Web handler is invoked** (due to registration order), providing caching and error handling. Domain handlers exist for testability and future extensibility.

**See [ADR-002](decisions/ADR-002-domain-features-architecture.md) for complete architectural rationale, alternatives considered, and implementation notes.**

## Web Layer

### Blazor Components & Pages

**Index.razor (BlogPosts/)**
- Lists all blog posts
- Shows title, author, creation date, publication status
- Links to Create, Edit, and Delete actions

**Create.razor (BlogPosts/)**
- Form to create a new blog post
- Fields: Title, Content, Author
- Defaults to draft status (IsPublished = false)

**Edit.razor (BlogPosts/)**
- Form to edit an existing post
- Pre-populates form with current values
- Publish/Unpublish toggle
- Delete button (with confirmation)

**ConfirmDeleteDialog.razor (Shared/)**
- Reusable modal component for delete confirmation
- Used by Edit and potentially other deletable entities
- Prevents accidental data loss

### Layout Components

**MainLayout.razor**
- Root container for all pages
- Includes NavMenu and page content area

**NavMenu.razor**
- Navigation links (Home, Blog Posts, etc.)
- Optional theme toggle for future enhancements

**ReconnectModal.razor**
- Aspire's built-in reconnection UI
- Handles transient disconnections gracefully

### Form Binding & Validation

- Use Blazor's `@bind` for two-way data binding
- Validate in the Domain layer when possible
- Keep UI logic in components (where it belongs)

## AppHost Layer

### Orchestration

AppHost wires together services using .NET Aspire:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add services
var domain = builder.AddProject<Projects.Domain>("domain");
var web = builder.AddProject<Projects.Web>("web")
    .WithReference(domain);  // Web depends on Domain

// Add service defaults (OpenTelemetry, health checks)
builder.AddServiceDefaults();

// Build and run
await builder.Build().RunAsync();
```

### Service Composition

- **AppHost** is the entry point
- **Web** is registered as a dependency of AppHost
- **Domain** is registered for access by Web
- **ServiceDefaults** configures OpenTelemetry, health checks, and other cross-cutting concerns

### Local Dashboard

Running `dotnet run` from AppHost launches the Aspire dashboard:
- View running services and their health
- Check logs and metrics
- Monitor resource usage

## Testing Strategy

### Unit Tests (Unit.Tests/)

**Scope**: Entity logic, repository behavior

**Example Tests**:
- `BlogPost.Create()` — Factory method creates correct entity
- `BlogPost.Publish()` — Marks post as published
- `InMemoryBlogPostRepository.AddAsync()` — Stores post and returns it on retrieval

**Framework**: xUnit + FluentAssertions + NSubstitute

### Architecture Tests (Architecture.Tests/)

**Scope**: Layer dependency rules

**Tests**:
1. Domain project must not reference Web
2. Web project must not reference AppHost

**Framework**: NetArchTest.Rules

These tests ensure the layering rules are never accidentally violated.

### Integration Tests (Integration.Tests/)

**Status**: Stubbed (placeholder for future)

**Future Purpose**: Test Aspire service composition, end-to-end flows, startup behavior

## Key Design Decisions

> Full rationale, alternatives considered, and implementation notes for each decision are in **[docs/decisions/ADR-001](decisions/ADR-001-architecture-decisions.md)**.

### 1. Vertical Slice Architecture + MediatR

Feature code is co-located in `Web/Features/<Feature>/` slices. MediatR dispatches commands and queries through a pipeline that includes validation and caching behaviors.

### 2. MongoDB via EF Core Adapter

`MongoDB.EntityFrameworkCore` provides a familiar DbContext/LINQ API over MongoDB. `IDbContextFactory<BlogDbContext>` is required for Blazor Server thread safety.

### 3. Caching at Handler Level (Redis)

`CachingBehavior<,>` is a MediatR pipeline behavior that caches DTOs (not entities) in Redis. Write handlers explicitly invalidate cache keys.

### 4. Auth0 Authentication

`Auth0.AspNetCore.Authentication` SDK handles OIDC/OAuth2 flows. Claims are mapped to ASP.NET Core roles for `[Authorize]` usage.

### 5. Role-Based Authorization (Author / Admin)

Two roles: **Author** (CRUD own posts) and **Admin** (CRUD all posts + manage roles via Auth0 Management API).

### 6. Short Project Names

Projects are named `AppHost`, `Domain`, `Web`, `ServiceDefaults`. Namespaces use `MyBlog.*` prefix via `RootNamespace` in `.csproj`.

### 7. TreatWarningsAsErrors

All projects set `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` to enforce clean, warning-free code.

## Learning Outcomes

After working through MyBlog, you'll understand:

1. **Service Orchestration** — How .NET Aspire composes MongoDB, Redis, and Web services
2. **Vertical Slice Architecture** — Co-locating feature code; MediatR pipeline behaviors
3. **Interactive Rendering** — Blazor Server component model and form binding
4. **MongoDB with EF Core** — DbContext mapping, IDbContextFactory, document modeling
5. **Distributed Caching** — Redis via IDistributedCache; cache-aside at the handler level
6. **Authentication & Authorization** — Auth0 OIDC integration, role-based access control
7. **Test-Driven Development** — Unit tests with mocks; integration tests with real infrastructure
8. **Entity Design** — Factory methods, immutability, business logic encapsulation

## Running the Application

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run all tests (should pass 9/9)
dotnet test

# Start the Aspire dashboard and Web service
cd src/AppHost
dotnet run
```

The Aspire dashboard will display the URL for the Web service. Open it in your browser to interact with the blog.

## Future Enhancements (Learning Extensions)

Potential additions for deeper learning:

- **API Layer**: Add a REST API (Minimal APIs) alongside Blazor Server, sharing MediatR handlers
- **Comments**: Extend BlogPost with a Comments vertical slice (new MongoDB collection)
- **Tags & Search**: Add Tags to BlogPost; Atlas Search for full-text search
- **E2E Tests**: Add Playwright tests for UI workflows (auth flows, CRUD forms)
- **Resource-Based Authorization**: Extend Author role to enforce post-ownership checks via ASP.NET Core resource handlers
- **CI/CD**: GitHub Actions pipeline with dotnet build, test, and Docker publish steps

These can be explored incrementally as new vertical slices without modifying existing feature code.

---

## Architecture Decisions

All significant design choices are documented in [`docs/decisions/`](decisions/index.md).

| ADR | Title | Status |
|-----|-------|--------|
| [ADR-001](decisions/ADR-001-architecture-decisions.md) | Core Architecture Decisions (VSA, MongoDB, Redis, Auth0, RBAC) | Accepted |
| [ADR-002](decisions/ADR-002-domain-features-architecture.md) | Domain/Features Layer Architecture (Two-Layer CQRS Pattern) | Accepted |

---

**Maintained by**: @mpaulosky  
**Project Status**: Training / Learning  
**Last Updated**: 2026-04-23
