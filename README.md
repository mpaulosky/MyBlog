# MyBlog

A hands-on learning project for .NET Aspire orchestration, Blazor Server rendering, and clean architecture.

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![MIT License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

> ⚠️ **Training Project** — This is a learning exercise focused on .NET practices, not a production application.

## Overview

MyBlog is a simple blog application demonstrating core .NET concepts: .NET Aspire orchestration, Blazor Server for interactive server rendering, clean architecture with domain/web layer separation, and test-driven development.

## Features

- **List Blog Posts** — View all published and draft posts with metadata
- **Create Posts** — New posts start in draft status
- **Edit Posts** — Update title and content anytime
- **Delete with Confirmation** — ConfirmDeleteDialog component prevents accidental deletions
- **Publish/Unpublish** — Toggle post publication status

## Technology Stack

- **.NET 10** with **C# 14**
- **.NET Aspire 13.2.2** — Service orchestration and health checks
- **Blazor Server (Interactive Server Rendering)** — Dynamic UI
- **xUnit** — Test framework
- **FluentAssertions** — Assertion library
- **NetArchTest.Rules** — Architecture validation
- **NSubstitute** — Mocking framework
- **In-Memory Repository** — No database (training project by design)

## Project Structure

```
MyBlog/
├── src/
│   ├── AppHost/              # .NET Aspire orchestration entry point
│   ├── Domain/               # BlogPost entity, IBlogPostRepository, InMemoryBlogPostRepository
│   ├── ServiceDefaults/      # Shared Aspire concerns (OpenTelemetry, health checks)
│   └── Web/                  # Blazor Server application
│       └── Components/
│           ├── Pages/BlogPosts/  # Index, Create, Edit pages
│           ├── Pages/            # Home, Error, NotFound
│           ├── Shared/           # ConfirmDeleteDialog
│           └── Layout/           # MainLayout, NavMenu, ReconnectModal
├── tests/
│   ├── Unit.Tests/           # 7 unit tests (BlogPost, Repository)
│   ├── Architecture.Tests/    # 2 architecture tests (layer rules)
│   └── Integration.Tests/     # Stubbed for future Aspire integration
├── docs/                     # Documentation
├── Directory.Build.props     # Centralized build settings
├── global.json               # SDK version lock
├── GitVersion.yml            # Versioning config
├── MyBlog.slnx               # Solution file
└── README.md
```

## Getting Started

### Prerequisites

- **.NET 10 SDK** — [Download](https://dotnet.microsoft.com/en-us/download)
- **Auth0 account** — required for authentication (free tier works). See [AUTH0_SETUP.md](docs/AUTH0_SETUP.md) for full setup instructions.

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

3. **Build the solution**

   ```bash
   dotnet build
   ```

4. **Run tests**

   ```bash
   dotnet test
   ```

5. **Run the application** (via Aspire AppHost)

   ```bash
   cd src/AppHost
   dotnet run
   ```

   The Aspire dashboard will be available at the URL shown in the console.

## Testing

All 9 tests pass. Test structure:

- **Unit.Tests/** — Entity logic and repository behavior (7 tests)
- **Architecture.Tests/** — Layer dependency enforcement (2 tests)
- **Integration.Tests/** — Placeholder for future Aspire integration tests

Run all tests:

```bash
dotnet test
```

## Learning Objectives

This project teaches:

1. **.NET Aspire** — Service orchestration, resource composition, health checks
2. **Blazor Server** — Interactive server rendering, component models, form handling
3. **Clean Architecture** — Domain/Web layer separation, repository pattern
4. **Test-Driven Development** — Unit tests, architecture tests, xUnit/FluentAssertions
5. **Entity Design** — Factory methods, immutable updates, domain logic
6. **Short Naming** — Repository-level context (MyBlog) vs project names (AppHost, Domain, Web)

## Documentation

- [ARCHITECTURE.md](docs/ARCHITECTURE.md) — Solution structure, layer diagram, design decisions
- [CONTRIBUTING.md](docs/CONTRIBUTING.md) — Contribution guidelines and project setup
- [AUTH0_SETUP.md](docs/AUTH0_SETUP.md) — Step-by-step Auth0 configuration for local development

## License

Licensed under the MIT License. See [LICENSE](LICENSE) file for details.

---

**Status**: Training Project | **.NET 10** | **Maintained by**: @mpaulosky
