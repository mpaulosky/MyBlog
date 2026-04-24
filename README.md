# MyBlog

A hands-on learning project for .NET Aspire orchestration, Blazor Server rendering, and clean architecture.

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![MIT License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![xUnit Tests](https://img.shields.io/badge/Tests-xUnit-blueviolet?logo=github)](https://github.com/mpaulosky/MyBlog/actions/workflows/squad-ci.yml)
[![Latest Release](https://img.shields.io/github/v/release/mpaulosky/MyBlog?logo=github&color=blue&label=Release)](https://github.com/mpaulosky/MyBlog/releases/latest)

[![CI/CD](https://github.com/mpaulosky/MyBlog/actions/workflows/squad-ci.yml/badge.svg)](https://github.com/mpaulosky/MyBlog/actions/workflows/squad-ci.yml)
[![Test Suite](https://github.com/mpaulosky/MyBlog/actions/workflows/squad-test.yml/badge.svg)](https://github.com/mpaulosky/MyBlog/actions/workflows/squad-test.yml)

[![CodeCov Coverage](https://codecov.io/gh/mpaulosky/MyBlog/branch/main/graph/badge.svg)](https://codecov.io/gh/mpaulosky/MyBlog)
[![Coverage Trend](https://img.shields.io/badge/Coverage-Trend-blue?logo=codecov)](https://codecov.io/gh/mpaulosky/MyBlog/commits/main)
[![Coverage Gate](https://img.shields.io/badge/Coverage%20Gate-≥80%25-brightgreen?logo=codecov)](https://github.com/mpaulosky/MyBlog/actions/workflows/squad-test.yml)

[![Open Issues](https://img.shields.io/github/issues/mpaulosky/MyBlog?color=0366d6)](https://github.com/mpaulosky/MyBlog/issues?q=is%3Aopen+is%3Aissue)
[![Closed Issues](https://img.shields.io/github/issues-closed/mpaulosky/MyBlog?color=6f42c1)](https://github.com/mpaulosky/MyBlog/issues?q=is%3Aclosed+is%3Aissue)
[![Open PRs](https://img.shields.io/github/issues-pr/mpaulosky/MyBlog?color=28a745)](https://github.com/mpaulosky/MyBlog/pulls?q=is%3Aopen+is%3Apr)
[![Closed PRs](https://img.shields.io/github/issues-pr-closed/mpaulosky/MyBlog?color=6f42c1)](https://github.com/mpaulosky/MyBlog/pulls?q=is%3Aclosed+is%3Apr)

> ⚠️ **Training Project** — This is a learning exercise focused on .NET practices, not a production application.

## Overview

MyBlog is a Blazor Server blog application demonstrating modern .NET patterns: Aspire orchestration, CQRS with MediatR, MongoDB persistence, Redis distributed caching, Auth0 authentication, TailwindCSS v4 theming, and comprehensive testing (unit, architecture, integration, bUnit, E2E).

## Features

- **Blog Management**: Create, edit, delete, publish/unpublish blog posts
- **CQRS with MediatR**: All blog operations handled by MediatR command/query handlers with FluentValidation pipeline
- **Redis Caching**: L1 (in-memory) + L2 (Redis distributed) cache via `IBlogPostCacheService` abstraction
- **Auth0 Authentication**: Secure login with Authorization Code + PKCE flow; Role-Based Authorization (Admin/User policies)
- **Blazor TailwindCSS Theming**: Dark/light/system modes, 4 color schemes (Blue, Red, Green, Yellow), localStorage persistence
- **Aspire Orchestration**: MongoDB + Redis as Aspire resources with health checks and OpenTelemetry

## Technology Stack

- **.NET 10** with **C# 14**
- **.NET Aspire 13.2.3** — Service orchestration, MongoDB + Redis resources, health checks
- **Blazor Server (Interactive Server Rendering)** — Dynamic UI with TailwindCSS v4 theming
- **MongoDB** with **MongoDB.EntityFrameworkCore** — Blog post persistence
- **Redis** — Distributed caching (L2 cache via `IDistributedCache`)
- **MediatR** — CQRS pattern (Create/Update/Delete/GetAll/GetById handlers)
- **FluentValidation** — Validation pipeline behavior
- **Auth0** — Authentication + role-based authorization
- **xUnit** + **FluentAssertions** + **NSubstitute** — Test framework
- **bUnit** — Blazor component testing
- **NetArchTest.Rules** — Architecture validation

## Project Structure

```
MyBlog/
├── src/
│   ├── AppHost/              # .NET Aspire orchestration (MongoDB + Redis resources)
│   ├── Domain/               # BlogPost entity, MediatR handlers, validators, IBlogPostCacheService
│   │   ├── Abstractions/     # Result<T>, IBlogPostRepository, IBlogPostCacheService
│   │   ├── Behaviors/        # ValidationBehavior MediatR pipeline
│   │   ├── Entities/         # BlogPost domain entity
│   │   └── Interfaces/       # Repository and cache interfaces
│   ├── ServiceDefaults/      # OpenTelemetry, health checks, Aspire extensions
│   └── Web/                  # Blazor Server application
│       ├── Features/BlogPosts/  # Vertical slice: commands, queries, Blazor pages
│       ├── Infrastructure/Caching/  # BlogPostCacheService (L1+L2 implementation)
│       ├── Components/Theme/ # TailwindCSS theme components
│       ├── Security/         # Auth0 endpoints
│       └── Styles/           # TailwindCSS input.css
├── tests/
│   ├── Unit.Tests/           # Domain entity + handler unit tests
│   ├── Architecture.Tests/   # Layer dependency enforcement
│   ├── Integration.Tests/    # Aspire integration tests
│   ├── AppHost.Tests/        # Aspire AppHost + E2E tests
│   ├── Web.Tests/            # Web layer unit tests
│   ├── Web.Tests.Bunit/      # Blazor component tests (bUnit)
│   └── Web.Tests.Integration/# Web integration tests
├── docs/                     # Documentation + GitHub Pages blog
├── Directory.Build.props     # Centralized build settings
├── Directory.Packages.props  # Centralized NuGet versioning (CPM)
├── global.json               # SDK version lock (.NET 10.0.202)
├── GitVersion.yml            # Semantic versioning config
└── MyBlog.slnx               # Solution file
```

## Getting Started

### Prerequisites

- **.NET 10 SDK** — [Download](https://dotnet.microsoft.com/en-us/download)
- **Node.js 18+** — for TailwindCSS compilation
- **Auth0 account** — required for authentication (free tier). See [AUTH0_SETUP.md](docs/AUTH0_SETUP.md)
- **MongoDB** — MongoDB Atlas or local instance (or let Aspire provision via Docker)
- **Redis** — Redis instance (or let Aspire provision via Docker)

### Setup

1. **Clone the repository**

   ```bash
   git clone https://github.com/mpaulosky/MyBlog.git
   cd MyBlog
   ```

2. **Restore .NET dependencies**

   ```bash
   dotnet restore
   ```

3. **Install npm dependencies** (for TailwindCSS)

   ```bash
   npm install
   ```

4. **Build the solution**

   ```bash
   dotnet build
   ```

5. **Run tests**

   ```bash
   dotnet test
   ```

6. **Run the application** (via Aspire AppHost)

   ```bash
   cd src/AppHost
   dotnet run
   ```

   The Aspire dashboard will be available at the URL shown in the console.

## Testing

Multiple test tiers, all passing:

- **Unit.Tests** — Domain entity logic and MediatR handler behavior
- **Architecture.Tests** — Layer dependency enforcement
- **Integration.Tests** — Aspire integration tests
- **AppHost.Tests** — .NET Aspire host + E2E tests
- **Web.Tests** — Web layer unit tests
- **Web.Tests.Bunit** — Blazor component tests (bUnit)
- **Web.Tests.Integration** — Web integration tests

Run all tests:

```bash
dotnet test
```

## Documentation

- [ARCHITECTURE.md](docs/ARCHITECTURE.md) — Solution structure, layer diagram, design decisions
- [CONTRIBUTING.md](docs/CONTRIBUTING.md) — Contribution guidelines and project setup
- [AUTH0_SETUP.md](docs/AUTH0_SETUP.md) — Step-by-step Auth0 configuration for local development
- [TESTING.md](docs/TESTING.md) — Test strategy and running instructions
- [THEMING.md](docs/THEMING.md) — TailwindCSS theme system guide
- [SECURITY.md](docs/SECURITY.md) — Security guidelines
- [CODE_OF_CONDUCT.md](docs/CODE_OF_CONDUCT.md) — Community standards
- [REFERENCES.md](docs/REFERENCES.md) — NuGet and npm package references

## Dev Blog

<!-- BLOG_START -->
| Date | Title | Tags |
|------|-------|------|
| 2026-04-24 | [Release: v1.2.0 — Redis Caching and L1/L2 Cache Strategy](docs/blog/2026-04-24-release-v1-2-0.md) | release, v1.2.0, redis, caching, aspire, sprint-5 |
| 2026-04-24 | [Release: v1.1.0 — Blazor Theme System with TailwindCSS v4](docs/blog/2026-04-24-release-v1-1-0.md) | release, v1.1.0, blazor, tailwind, theme, testing, sprint-4 |
| 2026-04-20 | [Release: v1.0.0 — Semantic Versioning and Production Readiness](docs/blog/2026-04-20-release-v1-0-0.md) | release, semver, ci, devops |
| 2026-04-20 | [Sprint 3: E2E Testing and CI Hardening](docs/blog/2026-04-20-sprint-3-e2e-tests-ci-hardening.md) | e2e, aspire, ci, testing, sprint-3 |
| 2026-04-20 | [Sprint 2: CQRS and MediatR Deep Dive](docs/blog/2026-04-20-sprint-2-cqrs-mediatr.md) | cqrs, mediatr, testing, domain, sprint-2 |
<!-- BLOG_END -->

## Release History

| Version | Date | Highlights |
|---------|------|------------|
| [v1.2.0](https://github.com/mpaulosky/MyBlog/releases/tag/v1.2.0) | 2026-04-24 | **Redis & Caching** — IBlogPostCacheService L1+L2, handler cache integration |
| [v1.1.0](https://github.com/mpaulosky/MyBlog/releases/tag/v1.1.0) | 2026-04-24 | **Themes & Testing** — TailwindCSS v4 themes, test project reorganization, E2E fixes |
| [v1.0.1](https://github.com/mpaulosky/MyBlog/releases/tag/v1.0.1) | 2026-04-20 | Automatic semver versioning enabled |
| [v1.0.0](https://github.com/mpaulosky/MyBlog/releases/tag/v1.0.0) | 2026-04-20 | First semantic version release |

## License

Licensed under the MIT License. See [LICENSE](LICENSE) file for details.

---

**Status**: Training Project | **.NET 10** | **v1.2.0** | **Maintained by**: @mpaulosky
