---
name: pre-push-test-gate
confidence: high
source: .github/hooks/pre-push
description: >
  Knowledge skill documenting the 5-gate pre-push hook that enforces build
  cleanliness and full test passage before any git push. The hook is installed
  at .git/hooks/pre-push and mirrors CI gates locally.
---

## Pre-Push Test Gate

### Overview

The pre-push hook (`.github/hooks/pre-push`) enforces **5 gates** that mirror CI. It runs automatically on every `git push` and blocks the push if any gate fails.

> 📋 **For setup and day-to-day usage, see:** `docs/CONTRIBUTING.md`

### The 5 Gates

| Gate | Name | What It Does | Blocks If |
|------|------|-------------|-----------|
| **0** | Branch protection | Checks current branch | Push is to `main` or `dev` |
| **1** | Untracked source files | Scans for untracked `.razor`/`.cs` files | Untracked source files found (prompts y/N) |
| **2** | Release build | `dotnet build MyBlog.slnx --configuration Release` | Build fails (3 retries) |
| **3** | Unit/Arch tests | Runs 2 test projects in Release mode | Any test project fails (3 retries) |
| **4** | Integration tests | Runs 1 integration test project (Docker required) | Any test project fails (3 retries) |

### Gate 3 — Unit Test Projects (2 total)

```
tests/Architecture.Tests/Architecture.Tests.csproj
tests/Unit.Tests/Unit.Tests.csproj
```

### Gate 4 — Integration Test Projects (1 total, Docker required)

```
tests/Integration.Tests/Integration.Tests.csproj
```

These use Testcontainers (MongoDb) and Aspire DCP. Docker daemon MUST be running.

### Retry Behavior

Gates 2, 3, and 4 allow **3 attempts**. Between attempts the hook pauses and prompts:
> "Fix the errors and press Enter to retry, or Ctrl+C to abort"

### Hook Installation

The hook source is committed at `.github/hooks/pre-push`. Install once per clone:

```bash
cp .github/hooks/pre-push .git/hooks/pre-push
chmod +x .git/hooks/pre-push
```

> ⚠️ Do NOT create inline hook scripts. Always copy from `.github/hooks/pre-push` to get the full 5-gate version.

### Failure Taxonomy (Known Patterns)

| Symptom | Root Cause | Fix |
|---------|-----------|-----|
| Warning treated as error (Gate 2) | `TreatWarningsAsErrors=true` in Directory.Build.props | Fix the warning — do not suppress |
| Architecture test failure (Gate 3) | Naming convention violation | Commands → `Command`, queries → `Query`, handlers → `Handler` (must be `sealed`), validators → `Validator` |
| bUnit test failure (Gate 3) | API change in bUnit 2.x | Use `Render<T>()` not `RenderComponent<T>()` |
| `DateTime` equality failure (Gate 3) | `Empty` property calls `DateTime.UtcNow` each time | Assert individual fields, not whole-record equality |
| Docker not running (Gate 4) | Testcontainers can't start | `sudo systemctl start docker` or start Docker Desktop |
| Container timeout (Gate 4) | Slow image pull or low resources | Pre-pull `mongo:7.0`; increase Docker memory |
| Untracked `.razor`/`.cs` (Gate 1) | New files not staged | `git add <files>` before pushing |

### Related Documents

- **Hook source:** `.github/hooks/pre-push`
- **Contributor guide:** `docs/CONTRIBUTING.md` (Initial Setup and Pre-Push Gate sections)
- **Build repair prompt:** `.github/prompts/build-repair.prompt.md`
- **Ceremonies:** `.squad/ceremonies.md` (Build Repair Check)
