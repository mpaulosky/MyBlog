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

### 2. Install Git Hooks (Automatic)

**Hooks are installed automatically on clone.** If needed, reinstall manually:

```bash
./scripts/install-hooks.sh
```

This installs a **pre-push gate** that validates your code before it reaches
GitHub. The hook also installs a post-checkout hook that ensures the pre-push
gate stays current as the repo evolves.

### What the Pre-Push Gate Enforces

The pre-push hook automatically runs before every `git push` and enforces
**7 sequential gates**:

| Gate | Rule | Enforced Behavior |
|------|------|--------|
| **0** | Squad branch naming | Rejects pushes on non-`squad/{issue}-{slug}` branches; blocks `main` and `dev` |
| **1** | Untracked source files | Warns if `.razor` or `.cs` files exist but are not staged; prompts to confirm before proceeding |
| **2** | Markdown lint | Runs `markdownlint-cli2`; blocks push on Markdown lint violations |
| **3** | dotnet format | Runs `dotnet format --verify-no-changes`; prompts to auto-fix if formatting issues found |
| **4** | Release build | Runs `dotnet build MyBlog.slnx --configuration Release`; zero warnings or errors required |
| **5** | Unit & architecture tests | Runs `Architecture.Tests`, `Domain.Tests`, `Web.Tests`, `Web.Tests.Bunit` (Release configuration) |
| **6** | Integration tests | Runs `Web.Tests.Integration`, `AppHost.Tests` (Release configuration; Docker daemon required) |

**Retry logic:** Gates 4–6 allow up to **3 attempts**. Between failures, the
hook pauses and prompts you to fix errors, then retries automatically.

### Branch Naming (Strict)

All work must be on a `squad/{issue}-{slug}` branch. Examples:

```bash
git checkout -b squad/42-fix-login-validation
git checkout -b squad/103-add-blog-search-feature
```

The pre-push hook rejects any push from a branch that does not match this
pattern, or from `main`/`dev`.

### Bypassing the Gate (Emergency Only)

```bash
git push --no-verify
```

⚠️ **PROHIBITED without prior written approval** from Ralph + Aragorn documented
in a GitHub issue comment. The hook blocks clearly broken code; skipping it
wastes CI cycles and bypasses team quality gates. Fix locally first.

## Development Workflow

### Prerequisites

- **.NET 10 SDK** — [Download](https://dotnet.microsoft.com/en-us/download)
- **Docker daemon** — Required for integration tests (Gate 6: `tests/Web.Tests.Integration`, `tests/AppHost.Tests`)
- **Auth0 account** — See [AUTH0_SETUP.md](AUTH0_SETUP.md) for Auth0 configuration

### Building and Testing Locally

```bash
# Restore dependencies
dotnet restore MyBlog.slnx

# Build the solution (Release config, as Gate 4 does)
dotnet build MyBlog.slnx --configuration Release

# Run all tests (as Gates 5 and 6 do)
dotnet test MyBlog.slnx --configuration Release

# Run the application (via Aspire AppHost)
cd src/AppHost
dotnet run
```

### Creating a Feature Branch

1. Start from `dev`:

```bash
git checkout dev
git pull origin dev
```

1. Create a `squad/*` branch with the issue number and a kebab-case slug:

```bash
git checkout -b squad/42-fix-login-validation
```

The pre-push gate will reject any other branch name.

### Pushing Your Work

Before `git push`:

1. Verify you are on a `squad/*` branch:

```bash
git symbolic-ref --short HEAD
```

1. Ensure all untracked `.razor` and `.cs` files are staged or intentionally
   excluded from git.

2. Run the pre-push gates locally to catch errors early:

```bash
# Run unit/architecture tests (Gate 4)
dotnet test tests/Architecture.Tests --configuration Release --no-build
dotnet test tests/Domain.Tests --configuration Release --no-build
dotnet test tests/Web.Tests --configuration Release --no-build
dotnet test tests/Web.Tests.Bunit --configuration Release --no-build
# Run integration tests (Gate 6 — requires Docker)
dotnet test tests/Web.Tests.Integration --configuration Release --no-build
dotnet test tests/AppHost.Tests --configuration Release --no-build
```

1. Push:

```bash
git push
```

If the pre-push gate fails, fix the errors and retry `git push`. The hook will
re-run all gates again.

### Pull Requests

1. Create a PR **from your `squad/*` branch to `dev`**:

```bash
gh pr create \
  --base dev \
  --title "feat(scope): description" \
  --body "Closes #<issue-number>

## Changes
- Your changes here

## Testing
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Manual testing done"
```

1. **Wait for CI to pass** — all checks must be green before requesting review.

2. Address code review feedback. If changes are needed, push corrections to the
   same branch.

3. Once all reviewers approve and CI is green, squash-merge to `dev`:

```bash
gh pr merge <PR-number> --squash --delete-branch
```

1. After merge, local branch cleanup is automatic via the post-checkout hook,
   but you can also manually clean up orphaned branches:

```bash
git checkout dev
git pull origin dev
git fetch --prune
git branch -vv | grep gone | awk '{print $1}' | xargs -r git branch -D
```

### After Your PR Is Merged

**⚠️ Important:** Do not keep committing on your `squad/*` branch after your PR
has been merged. If you need to continue work on related issues:

1. **Switch to `dev` and pull the latest:**

```bash
git checkout dev
git pull origin dev
```

1. **Create a fresh `squad/{issue}-{slug}` branch for your next task:**

```bash
git checkout -b squad/45-next-issue
```

1. **Push to the new branch** — the pre-push gate will guide you.

If you accidentally commit on a merged branch, you can recover by following these
same steps. New commits on a merged branch create orphaned history; starting fresh
on a new issue branch keeps the repository clean and your work tracking obvious.

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

## Resources and References

- [ARCHITECTURE.md](ARCHITECTURE.md) — Solution structure and design decisions
- [AUTH0_SETUP.md](AUTH0_SETUP.md) — Auth0 configuration guide
- [README.md](../README.md) — Project overview and getting started
- [Pre-Push Process Playbook](../.squad/playbooks/pre-push-process.md) — Detailed pre-push
  troubleshooting and gate descriptions
- [PR Review & Merge Process](../.squad/playbooks/pr-merge-process.md) — End-to-end PR
  lifecycle and reviewer protocol

## Troubleshooting

### Build Failures (Gate 4)

- **Warnings treated as errors:** The Release config enforces
  `TreatWarningsAsErrors=true`. Fix warnings first.
- **Missing file references:** Stage any new `.razor` or `.cs` files with
  `git add`, then retry.
- **NuGet restore failure:** Run `dotnet restore` manually and retry.

### Test Failures (Gates 5 & 6)

- **Architecture test failure:** Check naming conventions (commands →
  `Command`, queries → `Query`, handlers → `Handler`, validators →
  `Validator`).
- **DateTime equality failures:** Assert individual fields instead of
  whole-record equality; `UtcNow` changes between calls.
- **Docker not running (Gate 6):** Start Docker Desktop and retry.
- **Container startup timeout:** Increase Docker resources and verify
  images are pulled.

### Hook Not Installed

If the pre-push hook is missing:

```bash
./scripts/install-hooks.sh
```

## Questions?

Open an issue or reach out to [@mpaulosky](https://github.com/mpaulosky).
