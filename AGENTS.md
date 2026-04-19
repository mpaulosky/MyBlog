# AGENTS.md — AI Agent Orientation

## Architecture

- **Application model:** .NET 10 Blazor Web App using server-side rendering with
  interactive components.
- **Orchestration:** .NET Aspire AppHost in `src/AppHost` composes service
  dependencies for the web app.
- **Authentication:** Auth0 v8 integrations in the web layer
  (`Auth0.AspNetCore.Authentication` + `Auth0.ManagementApi`).
- **Styling:** TailwindCSS v4 build pipeline is wired through `npm run tw:build`
  during local `dotnet build`.
- **Current persistence state:** in-memory repository implementation is used for
  training workflows in the current sprint baseline.
- **Data:** MongoDB is planned as a primary persistence milestone in **Sprint 3**.
- **Caching:** Redis caching is planned for **Sprint 5**.

## Project Structure

- `src/Web` — Blazor web application (UI, auth, handlers, pages/components).
- `src/AppHost` — .NET Aspire orchestration entry point and resource wiring.
- `src/ServiceDefaults` — shared service defaults (telemetry, health, resilience).
- `tests/Architecture.Tests` — architecture guardrail tests.
- `tests/Unit.Tests` — unit and bUnit component tests.
- `tests/Integration.Tests` — integration tests (Docker-dependent scenarios).

## Key Files

- `MyBlog.slnx` — solution file linking all `src/*` and `tests/*` projects.
- `Directory.Build.props` — shared build properties (nullable, warnings as errors).
- `Directory.Packages.props` — centralized NuGet versions (reference point for
  centralized package management).
- `src/Web/appsettings.json` — Auth0 configuration (`Domain`, `ClientId`,
  `RoleClaimTypes`).
- `.squad/team.md` — squad roster, roles, and ownership map.

## Build & Test

```bash
dotnet build MyBlog.slnx --configuration Release
dotnet test MyBlog.slnx --configuration Release
```

## Squad

Use `.squad/team.md` as the source of truth for squad member roster, role
ownership, and handoff boundaries.

## Conventions

- Branch naming:
  - `squad/{issue}-{slug}` for feature work.
  - `sprint/{N}-{slug}` for sprint branches.
- Sprint 1 targeting: base integrations on `sprint/1-foundation`.
- Compiler/settings baseline: C# 13, nullable enabled, warnings as errors.
- Blazor changes should follow `.github/instructions/blazor.instructions.md`.
