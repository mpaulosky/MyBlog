# Contributing to MyBlog

Thank you for your interest in contributing to MyBlog! This document provides guidelines for setting up your development environment and submitting contributions.

## Initial Setup

### 1. Clone the Repository

```bash
git clone https://github.com/mpaulosky/MyBlog.git
cd MyBlog
```

### 2. Install Git Hooks

**IMPORTANT:** After cloning the repository, run the hook installation script:

```bash
./scripts/install-hooks.sh
```

This installs a **pre-push gate** that validates your code before it reaches GitHub.

### What the Pre-Push Gate Does

The pre-push hook automatically runs before every `git push`:

1. **Build** — `dotnet build MyBlog.slnx --no-incremental -c Release`
2. **Test** — `dotnet test MyBlog.slnx --no-build -c Release`

If either step fails, the push is aborted. This prevents broken code from reaching GitHub and ensures CI stays green.

### Bypassing the Gate (Emergency Only)

In rare cases where you need to push despite build/test failures:

```bash
git push --no-verify
```

⚠️ **Use sparingly** — This bypasses local validation. CI will still catch issues, but it's better to fix problems locally.

## Development Workflow

### Prerequisites

- **.NET 10 SDK** — [Download](https://dotnet.microsoft.com/en-us/download)
- **Auth0 account** — See [AUTH0_SETUP.md](docs/AUTH0_SETUP.md) for configuration

### Building and Testing

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run all tests
dotnet test

# Run the application (via Aspire AppHost)
cd src/AppHost
dotnet run
```

### Branch Strategy

- **`main`** — Release-only branch, protected with strict rules
- **`dev`** — Primary development branch (default)
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
- Use C# 14 features (file-scoped namespaces, record types, pattern matching)
- Keep methods focused and testable

### Testing

- Write unit tests for new domain logic
- Maintain or improve code coverage (currently >90%)
- Use xUnit, FluentAssertions, and NSubstitute

### Architecture Rules

- Domain layer must not depend on Web layer
- Use repository pattern for data access
- Keep Blazor components focused (presentation only)

## Resources

- [ARCHITECTURE.md](docs/ARCHITECTURE.md) — Solution structure and design decisions
- [AUTH0_SETUP.md](docs/AUTH0_SETUP.md) — Auth0 configuration guide
- [README.md](README.md) — Project overview and getting started

## Questions?

Open an issue or reach out to @mpaulosky.

---

**Remember:** Install git hooks with `./scripts/install-hooks.sh` after cloning!
