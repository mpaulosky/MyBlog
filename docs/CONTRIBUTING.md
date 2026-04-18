# Contributing to MyBlog

Thank you for your interest in contributing to MyBlog! This document is the
canonical contributor guide for project setup, workflow, and pull request
expectations.

## Initial Setup

### 1. Clone the Repository

```bash
git clone https://github.com/mpaulosky/MyBlog.git
cd MyBlog
```

### 2. Install Git Hooks

**Important:** After cloning the repository, run the hook installation script:

```bash
./scripts/install-hooks.sh
```

This installs a **pre-push gate** that validates your code before it reaches
GitHub.

### What the Pre-Push Gate Does

The pre-push hook automatically runs before every `git push` and enforces
**5 gates**:

| Gate | Name | Action |
|------|------|--------|
| **0** | Branch protection | Blocks direct pushes to `main` or `dev` |
| **1** | Untracked source files | Warns about untracked `.razor`/`.cs` files |
| **2** | Release build | `dotnet build MyBlog.slnx --configuration Release` |
| **3** | Unit/Arch tests | `tests/Architecture.Tests`, `tests/Unit.Tests` |
| **4** | Integration tests | `tests/Integration.Tests` (Docker required) |

Gates 2-4 allow up to 3 attempts. The hook pauses between failures so you can
fix and retry without restarting the whole push.

### Bypassing the Gate (Emergency Only)

In rare cases where you need to push despite build or test failures:

```bash
git push --no-verify
```

**Use this sparingly.** It bypasses local validation. CI will still catch
issues, but fixing them locally is preferred.

## Development Workflow

### Prerequisites

- **.NET 10 SDK** — [Download](https://dotnet.microsoft.com/en-us/download)
- **Docker** — Required for `tests/Integration.Tests`
- **Auth0 account** — See [AUTH0_SETUP.md](AUTH0_SETUP.md) for configuration

### Building and Testing

```bash
# Restore dependencies
dotnet restore MyBlog.slnx

# Build the solution
dotnet build MyBlog.slnx --configuration Release

# Run all tests
dotnet test MyBlog.slnx --configuration Release

# Run the application (via Aspire AppHost)
cd src/AppHost
dotnet run
```

### Branch Strategy

- **`main`** — Release-only branch, protected with strict rules
- **`dev`** — Primary development branch
- **`squad/*`** — Feature branches for squad members and Copilot agents

Create feature branches from `dev`:

```bash
git checkout dev
git pull origin dev
git checkout -b squad/my-feature
```

### Pull Requests

1. Create a PR from your `squad/*` branch to `dev`
2. Ensure all CI checks pass (build, tests, coverage)
3. Address code review feedback
4. Squash and merge once approved

## Code Standards

### .NET Conventions

- Follow .NET naming conventions (PascalCase for types, camelCase for locals)
- Use C# 14 features where appropriate
- Keep methods focused and testable

### Testing

- Write unit tests for new domain logic
- Maintain or improve code coverage
- Use xUnit, FluentAssertions, and NSubstitute

### Architecture Rules

- Domain layer must not depend on Web layer
- Use repository and feature-slice patterns already present in the solution
- Keep Blazor components focused on presentation concerns

## Resources

- [ARCHITECTURE.md](ARCHITECTURE.md) — Solution structure and design decisions
- [AUTH0_SETUP.md](AUTH0_SETUP.md) — Auth0 configuration guide
- [README.md](../README.md) — Project overview and getting started

## Questions?

Open an issue or reach out to
[@mpaulosky](https://github.com/mpaulosky).
