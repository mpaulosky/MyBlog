# Pre-Push Process Playbook

**Owner:** Boromir (DevOps) + Aragorn (Lead)
**Ref:** `.github/hooks/pre-push`, `CONTRIBUTING.md`
**Last Updated:** 2026-05-12

---

> ⛔ **HARD BLOCK — `git push --no-verify` is prohibited.**
> Bypassing the pre-push hook defeats all local quality gates (build, tests,
> coverage). CI becomes the first place failures are discovered — wasting
> everyone's time. **Fix the root cause instead:**
>
> - SDK mismatch → install the SDK version pinned in `global.json` from https://dot.net
> - Hook not installed → run `scripts/install-hooks.sh`
> - Docker not running → start Docker Desktop / `sudo systemctl start docker`
>
> Any `--no-verify` push requires **prior written approval from Ralph + Aragorn**
> documented in a GitHub issue comment. Undocumented bypasses are a retro action item.

## Overview

The pre-push hook (`.github/hooks/pre-push`) enforces 7 gates that mirror CI. This playbook documents what agents must do before pushing and how to troubleshoot failures.

## Pre-Flight Checklist (Before `git push`)

Before running `git push`, verify:

1. **You are on a `squad/*` branch** — Gate 0 blocks pushes to `main` and `dev`

   ```bash
   git symbolic-ref --short HEAD
   # Must show: squad/{issue}-{slug}
   ```

2. **No untracked `.razor` or `.cs` files** — Gate 1 blocks these (invisible to CI)

   ```bash
   git ls-files --others --exclude-standard -- '*.razor' '*.cs'
   # Must be empty. If files appear, stage them:
   git add <files>
   ```

3. **Markdown lint passes** — Gate 2 runs `markdownlint-cli2`

   ```bash
   npx markdownlint-cli2 "**/*.md" \
     "!**/node_modules/**" \
     "!.squad/**" \
     "!.copilot/**" \
     "!.github/agents/**" \
     "!.github/skills/**" \
     "!.github/copilot-instructions.md"
   ```

4. **Code is formatted** — Gate 3 runs `dotnet format --verify-no-changes`

   ```bash
   dotnet format MyBlog.slnx --verify-no-changes
   ```

   If formatting issues are found, fix with:

   ```bash
   dotnet format MyBlog.slnx
   git add -u
   git commit  # or --amend
   ```

5. **Release build passes locally** — Gate 4 runs Release (not Debug)

   ```bash
   dotnet build MyBlog.slnx --configuration Release
   ```

   If build fails, run `.github/prompts/build-repair.prompt.md` to fix.

6. **Unit tests pass** — Gate 5 runs 4 test projects

   ```bash
   dotnet test tests/Architecture.Tests/Architecture.Tests.csproj --configuration Release --no-build
   dotnet test tests/Domain.Tests/Domain.Tests.csproj --configuration Release --no-build
   dotnet test tests/Web.Tests/Web.Tests.csproj --configuration Release --no-build
   dotnet test tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj --configuration Release --no-build
   ```

7. **Docker is running** — Gate 6 requires Docker for integration tests

   ```bash
   docker info &>/dev/null && echo "Docker OK" || echo "Docker NOT running"
   ```

## The 7 Gates (What the Hook Runs)

When you execute `git push`, the hook runs automatically:

| Gate  | What                   | Blocks Push If                                                           |
| ----- | ---------------------- | ------------------------------------------------------------------------ |
| **0** | Branch protection      | Current branch is `main` or `dev`                                        |
| **1** | Untracked source files | `.razor`/`.cs` files not staged (prompts y/N)                            |
| **2** | markdownlint-cli2      | Any Markdown lint violation                                               |
| **3** | dotnet format          | Any file requires formatting changes (prompts auto-fix y/N)              |
| **4** | Release build          | `dotnet build --configuration Release` fails (3 attempts)                |
| **5** | Unit/Arch/bUnit tests  | Any of 4 test projects fail (3 attempts)                                 |
| **6** | Integration tests      | Any of 2 integration test projects fail; Docker not running (3 attempts) |

### Gate 5 — Test Projects (Unit)

```text
tests/Architecture.Tests/Architecture.Tests.csproj
tests/Domain.Tests/Domain.Tests.csproj
tests/Web.Tests/Web.Tests.csproj
tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj
```

### Gate 6 — Integration Test Projects (Docker Required)

```text
tests/Web.Tests.Integration/Web.Tests.Integration.csproj
tests/AppHost.Tests/AppHost.Tests.csproj
```

These use Testcontainers (mongo:7.0) and Aspire DCP (`DistributedApplicationTestingBuilder`). Docker daemon MUST be running.

## Retry Behavior

The hook allows **3 attempts** for Gates 4, 5, and 6. Between attempts:

- The hook pauses and prompts "Fix the errors and press Enter to retry, or Ctrl+C to abort"
- Fix the failing code, then press Enter
- The gate re-runs from scratch

## Troubleshooting

### Markdownlint Failure (Gate 2)

| Symptom                    | Fix                                                                     |
| -------------------------- | ----------------------------------------------------------------------- |
| MD013 / line-length errors | Reflow paragraphs or use explicit markdownlint disable comments         |
| Lint binary missing        | Run `npm install` to restore `markdownlint-cli2` dev dependency         |
| Unexpected lint scope      | Use `.markdownlint.json` and keep hook globs aligned with CI exclusions |

### Formatting Failure (Gate 3)

| Symptom                  | Fix                                                               |
| ------------------------ | ----------------------------------------------------------------- |
| Files differ from format | Run `dotnet format MyBlog.slnx`, then `git add -u && git commit` |
| Analyzer rule violation  | Run `dotnet format MyBlog.slnx --diagnostics <rule-id>` to debug |
| dotnet format not found  | Install .NET SDK matching `global.json`; format ships with SDK   |

### Build Failure (Gate 4)

| Symptom                  | Fix                                                   |
| ------------------------ | ----------------------------------------------------- |
| Warning treated as error | Fix the warning — `TreatWarningsAsErrors=true` is set |
| Missing file reference   | Stage all new `.razor`/`.cs` files (Gate 1 issue)     |
| NuGet restore failure    | Run `dotnet restore` manually first                   |

**Escalation:** Run `.github/prompts/build-repair.prompt.md` for automated fix.

### Test Failure (Gate 5)

| Symptom                   | Fix                                                                                                                |
| ------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| Architecture test failure | Check naming conventions (commands → `Command`, queries → `Query`, handlers → `Handler`, validators → `Validator`) |
| bUnit test failure        | Verify Blazor component rendering; check `Render<T>()` not `RenderComponent<T>()` (bUnit 2.x)                      |
| DateTime equality failure | Assert individual fields, not whole-record equality (UtcNow varies between calls)                                  |

### Integration Test Failure (Gate 6)

| Symptom                   | Fix                                                             |
| ------------------------- | --------------------------------------------------------------- |
| Docker not running        | Start Docker Desktop or `sudo systemctl start docker`           |
| Container startup timeout | Increase Docker resources; check `mongo:7.0` image is pulled    |
| Connection string error   | Set `MONGODB_CONNECTION_STRING` env var if custom config needed |

### Hook Not Installed

The hook must be at `.git/hooks/pre-push`. The repo provides the hook at `.github/hooks/pre-push`. Install:

```bash
cp .github/hooks/pre-push .git/hooks/pre-push
chmod +x .git/hooks/pre-push
```

## Anti-Patterns

- ❌ **Bypassing the hook** with `git push --no-verify` — CI will catch it, wasting time
- ❌ **Committing broken Markdown** — Gate 2 blocks the push; run `npx markdownlint-cli2`
- ❌ **Committing unformatted code** — Gate 3 blocks the push; run `dotnet format MyBlog.slnx` first
- ❌ **Running Debug build only** — CI uses Release; Debug hides missing files
- ❌ **Pushing without Docker** — Gate 6 will block; start Docker first
- ❌ **Ignoring untracked files** — They're invisible to CI and will cause failures
- ❌ **Committing to `main` directly** — Gate 0 blocks this; use `squad/{issue}-{slug}` branches

## Related Documents

- **Hook source:** `.github/hooks/pre-push`
- **Build repair:** `.github/prompts/build-repair.prompt.md`
- **Contributing guide:** `CONTRIBUTING.md` (Pre-Push Gates section)
- **Ceremonies:** `.squad/ceremonies.md` (Build Repair Check, Standard Task Workflow Phase 3)
- **Skill:** `.squad/skills/pre-push-test-gate/SKILL.md`

---

**Use this playbook every time you push.** The hook enforces these gates automatically, but understanding them helps you fix failures faster.
