# Sam's Work History

## 2026-05-26 — Issue #350: Repair Aspire Startup on New Machine

### Task

Investigate AppHost, Web, and ServiceDefaults startup wiring for anything likely to fail on a fresh machine. Implement the smallest correct fix for any Sam-owned defect.

### Finding

**Root cause found and fixed — single MSBuild target gap in `src/Web/Web.csproj`.**

On a fresh machine, `dotnet build` fails with:

```bash
EXEC : error : Can't resolve 'tailwindcss'
error MSB3073: The command "npm run tw:build" exited with code 1.
```

The existing `BuildTailwind` MSBuild target calls `npm run tw:build` without first ensuring `node_modules` exists. `tailwind.css` is gitignored and never committed, so the first build always requires npm packages. The `CI != 'true'` guard correctly skips this on CI but does nothing for a fresh local machine.

**All other wiring verified clean:**

| Check | Result |
| --- | --- |
| `AppHost.cs` — MongoDB 7 tag, `mongo-data-v7` volume, `.WaitFor(mongo)` | ✅ Correct |
| `Program.cs` — `AddMongoDBClient`, `AddDbContextFactory`, Redis cache, all DI registrations | ✅ Correct |
| `ServiceDefaults/Extensions.cs` — OpenTelemetry, health checks, service discovery | ✅ Correct |
| `public partial class Program {}` present in both AppHost and Web | ✅ Correct |
| Auth0 — mock fallback for Development/Testing environments prevents startup crash | ✅ Correct |
| Health endpoints guarded by Development/Testing environment only | ✅ Correct |

### Changed Files

- `src/Web/Web.csproj` — Added `EnsureNpmPackages` MSBuild target that runs `npm install` automatically when `node_modules` is missing

### Fix

Added a new `EnsureNpmPackages` target that fires `BeforeTargets="BuildTailwind"` with guard `Condition="'$(CI)' != 'true' AND !Exists(...node_modules)"`. This self-heals a fresh checkout with zero cost on subsequent builds and zero risk to CI.

### Validation Performed

- ✅ `dotnet build src/Web/Web.csproj -c Release` — 0 errors (ran npm install + tw:build automatically)
- ✅ `dotnet test tests/Web.Tests/Web.Tests.csproj -c Release` — 210/210 passed
- ✅ `dotnet test tests/Architecture.Tests/Architecture.Tests.csproj -c Release` — 16/16 passed

### Decision Recorded

`.squad/decisions/inbox/sam-npm-auto-install-on-fresh-machine.md`

### Notes for Boromir

All runtime wiring (AppHost, ServiceDefaults) is correct. There are no brittle container assumptions in Sam-owned code. Boromir should verify:

1. Docker Desktop is running (required for Aspire DCP to launch MongoDB 7 and Redis containers)
2. `mongo-data-v7` volume exists fresh (no MongoDB 8.x compatibility metadata from a prior machine)
3. Aspire DCP version matches `Aspire.AppHost.Sdk/13.3.5` — mismatched DCP can cause silent startup hangs

---

## 2026-05-27 — Issue #350 Follow-up: Analyzer Warning Cleanup in MongoDbResourceBuilderExtensions.cs

### Task

Boromir flagged ~37 pre-existing analyzer warnings in `src/AppHost/MongoDbResourceBuilderExtensions.cs` that were blocking the zero-warning build gate.

### Warnings Found and Fixed

| Rule | Count | Fix Applied |
| --- | --- | --- |
| CA2007 | 14 | Added `.ConfigureAwait(false)` to every `await` expression |
| CA1848 | 14 | Replaced all `LoggerExtensions.*` call-sites with `[LoggerMessage]` source-generated delegates |
| CA1873 | 5 | Resolved automatically by `[LoggerMessage]` fix (same call-sites as CA1848) |
| CA1305 | 1 | `sb.AppendLine(CultureInfo.InvariantCulture, $"...")` in stats loop |

### Approach

- Made the class `internal static partial class` (required for `[LoggerMessage]` source generation).
- Declared 12 `[LoggerMessage]`-attributed `private static partial void` methods covering every distinct log call across the three commands (Clear, Seed, Stats).
- The `LogConnectionStringError` delegate is shared by all three commands (same message, same parameter signature).
- Added `using System.Globalization;` for `CultureInfo.InvariantCulture`.

### Validation

- ✅ `dotnet build src/AppHost/AppHost.csproj -c Release --no-incremental` — zero warnings from `MongoDbResourceBuilderExtensions.cs`
- ✅ `dotnet test tests/AppHost.Tests/AppHost.Tests.csproj -c Release` — 53 passed, 1 skipped (pre-existing), 0 failed
- ✅ `dotnet test tests/Architecture.Tests/Architecture.Tests.csproj -c Release` — 16/16 passed

### Notes for Gimli

Behavior is identical — all log messages, levels, and parameters are preserved; only the call pattern changed from extension methods to source-generated delegates. No new test cases needed, but the mutex-concurrency tests in `AppHost.Tests` cover the command paths and all remain green.

---

## 2026-05-19 — Issue #348: Resolve Remaining Database Runtime Issues (branch squad/348-resolve-database-runtime-issues)

### Task

Re-investigate remaining database runtime issues after PR #346 (MongoDB 7 pin + fresh volume) and PR #347 (docs). Determine whether any Sam-owned backend defect remains.

### Finding

**No remaining backend defect found.** All Sam-owned code is correct.

Thorough audit performed:

| Check | Result |
| --- | --- |
| `AddMongoDBClient("myblog")` → `AddDbContextFactory<BlogDbContext>` wiring | ✅ Correct |
| `MongoDbBlogPostRepository` — all CRUD methods use short-lived IDbContextFactory contexts | ✅ Correct |
| `MongoDbCategoryRepository` — all CRUD methods use short-lived IDbContextFactory contexts | ✅ Correct |
| `BlogDbContext.OnModelCreating` — blogposts, categories, owned Author, CategoryId element name | ✅ Correct |
| AppHost.cs — MongoDB 7 tag, mongo-data-v7 volume, `.WaitFor(mongo)` before web | ✅ Correct |
| Unit tests (Web.Tests) — 210/210 passed | ✅ |
| Architecture tests — 16/16 passed | ✅ |
| Integration tests (Web.Tests.Integration) — 29/29 passed | ✅ |
| AppHost.Tests non-Docker tests — 20/20 passed | ✅ |
| Build (Release) — 0 errors | ✅ |

**Unstaged file in worktree:** `tests/AppHost.Tests/MongoSeedDataIntegrationTests.cs` contains an uncommitted test `SeedMyBlogData_Makes_Seeded_Posts_Visible_On_The_Blog_Page` (added during investigation). This is a **Gimli-owned** test file; Sam does NOT commit or modify it.

**Seed data GUID format:** The AppHost seed command uses `GuidRepresentation.Standard` (BinData subtype 4) for all GUID fields. `MongoDB.EntityFrameworkCore 10.0.1` also uses Standard UUID by default. No format mismatch.

**Cache layer (BlogPostCacheService):** L1 (IMemoryCache, 1 min) + L2 (Redis, 5 min). After a seed, the first blog page request hits MongoDB correctly since no prior cache entry exists for the session. Cache staleness is not a production bug — it is expected TTL behaviour.

### Changed Files

None. No production code change required.

### Validation Performed

- ✅ `dotnet build MyBlog.slnx -c Release` — 0 errors
- ✅ `dotnet test tests/Web.Tests/Web.Tests.csproj -c Release` — 210/210 passed
- ✅ `dotnet test tests/Architecture.Tests/Architecture.Tests.csproj -c Release` — 16/16 passed
- ✅ `dotnet test tests/Web.Tests.Integration/Web.Tests.Integration.csproj -c Release` — 29/29 passed
- ✅ AppHost non-Docker unit tests (MongoDb container config + seed/clear/stats command model tests) — 20/20 passed

### Recommendation

The runtime "remaining issues" after PR #346 are an **AppHost/Docker/runtime verification concern**, not a backend code defect:

1. The new test `SeedMyBlogData_Makes_Seeded_Posts_Visible_On_The_Blog_Page` (Gimli's file) is the canary that verifies the end-to-end path. Gimli should commit and run it in a Docker-enabled environment.
2. Boromir should verify that the `mongo-data-v7` Docker volume is fresh (no MongoDB 8.x compatibility metadata) on any machine that previously ran the old `mongo-data` volume config.
3. If the Aspire health checks time out in CI, Boromir should check DCP health-check configuration for the MongoDB 7 container.

---

## 2026-05-23 — Issue #350 Follow-up: Remaining AppHost.Tests Mongo Startup Failures

### Task

Investigate 3 remaining `AppHost.Tests` Mongo/Aspire startup failures that Gimli
identified after the first repair pass (exit code 100, `needRepair`).

### Findings

**Root cause: dirty `mongo-data-v7` Docker volume — not a code defect.**

All Sam-owned AppHost code (`AppHost.cs`, `MongoDbResourceBuilderExtensions.cs`,
`ClearCommandAppFixture.cs`) is correct. The failures came from an environment
state problem, not a code path bug.

| Check | Result |
| --- | --- |
| `AppHost.cs` — MongoDB 7 tag, `mongo-data-v7` volume, `.WaitFor(mongo)` | ✅ Correct |
| `MongoDbResourceBuilderExtensions.cs` — Clear/Seed/Stats commands, `_dbMutex` | ✅ Correct |
| `ClearCommandAppFixture` — starts Aspire, waits for `KnownResourceStates.Running` | ✅ Correct logic |
| Unit/model AppHost tests (22 tests) | ✅ 22/22 passed |
| MongoClearDataIntegrationTests (5 tests) | ✅ 5/5 passed |
| MongoShowStatsIntegrationTests (3 tests) | ✅ 3/3 passed |
| MongoSeedDataIntegrationTests (4 tests) | ✅ 4/4 passed |
| All 12 integration tests combined | ✅ 12/12 passed |

**Exit code 100 = MongoDB `needRepair` — WiredTiger found a dirty data directory.**

When the Aspire host is forcefully stopped (SIGKILL, machine sleep, or Aspire
`DisposeAsync()` racing ahead of the container's clean shutdown), MongoDB's
WiredTiger engine does not complete its shutdown sequence. The `mongod.lock`
file and WiredTiger checkpoint files are left in a state that causes the NEXT
MongoDB container mounting the same `mongo-data-v7` volume to exit immediately
with code 100.

**Reproduction scenario:**

1. `MongoStatsIntegration` fixture starts → MongoDB container A mounts `mongo-data-v7` → tests run → `App.DisposeAsync()` called
2. If Docker stops container A with SIGKILL (or close-before-flush), WiredTiger lock remains in volume
3. `MongoSeedIntegration` fixture starts → MongoDB container B mounts same `mongo-data-v7` → finds dirty lock → exits 100

This is **timing-dependent**: if `DisposeAsync()` waits long enough for
WiredTiger's graceful SIGTERM shutdown, the lock is removed and the next fixture
starts clean. In our test run, the lock was left from a previous unclean session
(not from within the same `dotnet test` invocation).

**After environment cleanup, all tests pass across multiple runs.**

### Changed Files

None. No AppHost or backend code changes required.

### Latent Risk and Recommendation for Gimli

`ClearCommandAppFixture` uses the same `mongo-data-v7` named volume as the
production AppHost. If multiple collections race or the host is killed uncleanly,
the volume becomes dirty and blocks the next collection's fixture.

**Suggested fix in `ClearCommandAppFixture.DisposeAsync()` (Gimli owns this):**

Option A — Use a test-unique volume name by removing the `ContainerMountAnnotation`
for `/data/db` after `DistributedApplicationTestingBuilder.CreateAsync()` and
replacing it with an anonymous volume. This eliminates all cross-fixture
contamination.

Option B — Add a brief `await Task.Delay(...)` after `App.DisposeAsync()` to
allow the Docker container time to flush WiredTiger before the next fixture
mounts the volume.

**Sam does not own the test file; Gimli must make this call.**

### Notes

- The `mongo-data-v7` volume design is correct for production (data persistence
  across Aspire restarts). The issue only manifests in test scenarios where
  3 separate xUnit fixtures each boot and tear down a full Aspire host using the
  same volume.
- A machine-level clean-up (`docker volume rm mongo-data-v7 && docker volume create mongo-data-v7`) resolves the dirty state and brings tests green.

---

## 2026-05-19 — Issue #345: AppHost MongoDB Container Crash Investigation (branch squad/345-fix-apphost-mongodb-crash)

### Task

Review whether Web/MongoDB application wiring contributes to the `mongo:8.2` container crash (exit 139 / SIGSEGV) observed in the Aspire runtime.

### Finding

**Root cause is purely AppHost/container — no Web wiring defect found.**

- `docker.io/library/mongo:8.2` (default for `Aspire.Hosting.MongoDB` 13.3.3) exits with code 139 (SIGSEGV). This is a container image crash — architecture incompatibility or a bug in the `8.2` tag. This is Boromir's domain.
- Web's MongoDB.Driver heartbeat timeouts and operation cancellations are **downstream symptoms** of the container crash, not a contributing cause.

**Web wiring verified clean:**

| Check | Result |
| --- | --- |
| `AddMongoDBClient("myblog")` matches AppHost database resource name | ✅ Correct |
| `AddDbContextFactory<BlogDbContext>` consumes IMongoClient correctly | ✅ Correct |
| `MongoDbBlogPostRepository` uses short-lived contexts from factory | ✅ Correct |
| `MongoDbCategoryRepository` uses short-lived contexts from factory | ✅ Correct |
| `BlogDbContext` — blogposts collection, Version concurrency token, categories unique index | ✅ Correct |
| AppHost `.WaitFor(mongo)` prevents Web serving before MongoDB is ready | ✅ Correct |

### Changed Files

None. No Web production code changes required.

### Validation Performed

- ✅ `dotnet build MyBlog.slnx -c Release` — 0 errors
- ✅ `Architecture.Tests` — 16/16 passed
- ✅ `Web.Tests` — 210/210 passed
- ✅ `Domain.Tests` — 67/67 passed

### Recommendation for Boromir

Pin AppHost to MongoDB 7 and use a fresh MongoDB 7 data volume in AppHost.cs.
MongoDB 8.x is the affected family in this environment because the container
expects AVX-capable CPUs and crashes before the app can connect.

```csharp
var mongo = builder.AddMongoDB("mongodb")
    .WithImageTag("7")
    .WithDataVolume("mongo-data-v7")
    .WithMongoExpress();
```

If a machine has already started MongoDB 8.x with the legacy `mongo-data`
volume, do not reuse that volume for MongoDB 7. The old volume can retain
MongoDB 8 feature compatibility version metadata, and MongoDB 7 can then exit
with code 62 during startup.

---

## 2026-05-15 — Issue #339: Category Backend (branch squad/339-category-backend)

### Task

Implement the full backend for the Category feature (domain, data, CQRS handlers, seed data).

### Changes Made

**Domain:**

- `Category.cs` entity — Name/Description, `Create` + `Update` with whitespace trimming
- `ICategoryRepository.cs` — GetById, GetAll, ExistsByName, ExistsByNameExcluding, Add, Update, Delete
- `IBlogPostRepository.cs` — added `ExistsByCategoryAsync` for safe-delete guard
- `BlogPost.cs` — added `CategoryId (Guid?)`, `AssignCategory`, `RemoveCategory`

**Web/Data:**

- `MongoDbCategoryRepository.cs` — EF Core LINQ over `BlogDbContext.Categories`
- `BlogDbContext.cs` — Categories DbSet + unique index on Name; CategoryId element on BlogPost
- `BlogPostDto.cs` / `BlogPostMappings.cs` — include `CategoryId`
- `CategoryDto.cs` / `CategoryMappings.cs`

**Features/Categories:**

- List / GetById / Create / Edit / Delete — all following CQRS + Result<T> + FluentValidation pattern
- `DeleteCategoryHandler` checks `ExistsByCategoryAsync` before deleting; returns `ResultErrorCode.Conflict`
- `CreateCategoryHandler` / `EditCategoryHandler` check name uniqueness; return `ResultErrorCode.Conflict`

**Program.cs:** Registered `MongoDbCategoryRepository` / `ICategoryRepository`

**AppHost seed:** Seeds `General` category (stable Guid `00000000-...-0001`) and assigns all seeded posts

**Tests:** Updated `BlogPostDto` construction in all test fixtures to pass `null` for new `CategoryId` param

### Build Validation

- ✅ `dotnet build MyBlog.slnx --configuration Release` — 0 errors
- ✅ `dotnet format --verify-no-changes` — clean
- ✅ `Architecture.Tests` — 16/16 passed
- ✅ `Domain.Tests` — 67/67 passed (includes CategoryTests + BlogPostCategoryTests)
- ✅ `Web.Tests` — 187/187 passed (includes all Category handler/validator tests)
- ✅ `Web.Tests.Bunit` — 101/101 passed

### Decision filed

`.squad/decisions/inbox/sam-issue339-backend.md`

## Learnings

### Pre-existing untracked test files stay out of the commit if not relevant to scope

Some test and skill files in the working tree (aragorn/boromir history files, a gh-pr-comments skill) had lint violations and were pre-staged when `git add -A` was run. The commit hook blocked the commit. Pattern: always `git status` before `git add -A` and selectively stage only backend-scope files, or unstage non-scope files after `git add -A`.

### Nullable CategoryId is the right additive pattern for optional FK on existing entities

Adding `Guid? CategoryId` with explicit `AssignCategory`/`RemoveCategory` domain methods preserves existing behavior (tests pass with `null`) while giving Legolas and the UI a clean optional-becomes-required upgrade path without a breaking API change.

### MongoDB 8.x crashes here; the safe fix is MongoDB 7 with a fresh v7 volume

When diagnosing MongoDB container crashes under Aspire, check the image tag
first. In this environment, the MongoDB 8.x family is the crashing family
because those images require AVX-capable CPUs. The safe fix is to pin AppHost
with `.WithImageTag("7")` and use `.WithDataVolume("mongo-data-v7")`.

Do not reuse the legacy `mongo-data` volume after a MongoDB 8.x run. That
volume can retain MongoDB 8 feature compatibility version metadata, which can
make MongoDB 7 exit with code 62 during startup. Web timeout logs remain
as downstream symptoms of the container crash and should not be mistaken for
wiring bugs.

### "Remaining database runtime issues" after container fix are runtime-verification concerns, not backend bugs

When a database container crash is fixed (PR #346), downstream "remaining issues" often turn out to be end-to-end runtime verification gaps, not new backend code defects. The correct response is:

1. Re-run the full non-Docker test suite to confirm baseline (unit + integration + architecture all pass).
2. Audit each repository and DbContext mapping against the seed data format — check GUID representation, field name alignment, and owned-entity mapping.
3. If all tests pass and code is correct, defer the runtime-only scenario to an AppHost/Docker integration test (Gimli) and Boromir for environment verification.
4. Never block a PR waiting for a Docker-requiring test when all code-level tests are green.

### Seed command GUID representation must match MongoDB.EntityFrameworkCore serialisation

The AppHost seed command writes documents via the raw MongoDB driver. Always use `GuidRepresentation.Standard` (BinData subtype 4) for all GUID fields, which matches `MongoDB.EntityFrameworkCore 10.0.1`'s default serialisation. Using a legacy representation (subtype 3) would cause EF Core `GetAllAsync` to deserialise zeros or throw.

---

## 2026-05-15 — PR #338: Skill Template Compliance Fix

### Task

Address blockers on PR #338 (`.squad/skills/self-authored-pr-gate/SKILL.md`):

1. Add missing front matter fields (`domain` and `source`) per repo skill template expectations
2. Adjust Codecov gating language to align with policy semantics (investigate/explain vs. unconditional hard block)

### Changes Made

1. **Front matter:** Added `domain: "PR governance, code review, CI/CD"` and `source: "earned"`
2. **Codecov check (Required Checks #3):** Changed from unconditional ">= 1% decrease blocks merge" to "...or any significant coverage decrease (≥1%) is investigated and explained"
3. **Do Not Use section:** Changed from "Codecov regression is >= 1% and unexplained" to "Codecov shows unexplained coverage decreases (no investigation provided)"

### Technical Rationale

- **Front matter alignment:** Repository skill templates expect both `domain` and `source` fields for classification and traceability
- **Codecov semantics:** The playbook gate description "coverage gate passing" is a binary CI check; the skill interprets policy as allowing-with-justification rather than outright blocking, which aligns with practical squad governance patterns where domain specialists can explain regression rationale

### Build Validation

- ✅ `markdownlint-cli2` passed on SKILL.md
- ✅ All pre-push gates passed
- ✅ Commit pushed to `squad/337-archive-self-authored-pr-gate`

### PR

PR #338 (blocker fixes)

## Learnings

### Skill Front Matter Is Structural, Not Optional

- Squad repository skill files follow a template with mandatory fields: `name`, `description`, `domain`, `source`, `confidence`
- Missing fields break alignment with repo tooling expectations and cause Copilot review flags
- Each field serves a distinct purpose: `domain` enables skill classification/discovery; `source` tracks whether the skill is "inherited" (from best practices) or "earned" (from repo-specific discoveries)

### Codecov Policy Intent vs. Policy Wording

- The PR gate playbook specifies "coverage gate passing" as a binary CI check — success or failure
- The self-authored-pr-gate skill must distinguish between "gate passes" (CI is green) and "gate interpretation" (why coverage passed/failed)
- Actual squad practice: a ≥1% decrease in coverage may be acceptable *if* the PR author explicitly documents why in the PR body (e.g., "refactoring reduces lines by X but maintains test coverage for Y"). The key gate is not the threshold but *transparency*.
- Revised wording ("investigated and explained") makes this intent clear without requiring a separate policy doc

## 2026-04-17: Optimistic Concurrency Implementation

### Task: Implement Optimistic Concurrency — Handlers + UI

Added `DbUpdateConcurrencyException` handling to Edit and Delete MediatR handlers, working alongside Aragorn who is implementing the database-layer concurrency token configuration.

### Changes Made

1. **EditBlogPostHandler.cs**: Added catch block for `DbUpdateConcurrencyException` returning `Result.Fail` with `ResultErrorCode.Concurrency`
2. **DeleteBlogPostHandler.cs**: Added catch block for `DbUpdateConcurrencyException` returning `Result.Fail` with `ResultErrorCode.Concurrency`
3. **Edit.razor**: Added UI to display concurrency warning alert when error code indicates concurrency conflict
4. **Index.razor**: Added UI to display concurrency warning alert in delete flow when error code indicates concurrency conflict

### Technical Decisions

- Used `global::Domain.Abstractions.ResultErrorCode.Concurrency` fully qualified name in Razor files to avoid conflicts with the Web project's RootNamespace setting (`MyBlog.Web`)
- Positioned concurrency catch before generic Exception catch to ensure specific handling
- Displayed user-friendly messages explaining the conflict and suggesting to reload

### Build Validation

- ✅ `dotnet build src/Web/Web.csproj` succeeded with 0 errors, 0 warnings

## Learnings

### Namespace Resolution in Blazor Razor Components

- When a project has a `<RootNamespace>` setting (e.g., `MyBlog.Web`), the Blazor compiler prepends that namespace to relative `@using` directives in `.razor` files
- Using `@using Domain.Abstractions` in a Razor file within the Web project tries to resolve to `MyBlog.Web.Domain.Abstractions`, not `Domain.Abstractions`
- Solution: Use fully qualified names with `global::` prefix when referencing external namespaces: `global::Domain.Abstractions.ResultErrorCode`
- This is specific to Blazor Razor compilation; regular C# files in the same project don't have this issue with `using Domain.Abstractions;`

### DbUpdateConcurrencyException Handling

- `Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException` is thrown when SaveChanges detects a concurrency conflict
- Best practice: Catch this specific exception before catching generic `Exception` to provide targeted error handling
- Return domain-appropriate Result with error code for UI to handle gracefully

### Result Pattern + Error Codes

- `Result.Fail(message, ResultErrorCode)` allows structured error handling
- UI can check `result.ErrorCode` to display context-specific messages
- Keeps business logic concerns (concurrency) separate from presentation (warning vs. error styling)

## 2026-04-18: Auth0 Configuration Error Handling

### Task: Fix Auth0 startup crash (empty ClientId)

### Root Cause

Case A: `appsettings.json` has empty strings for `Auth0:Domain` and `Auth0:ClientId`. No user secrets were set for the Web project. The Auth0 SDK's OpenIdConnect options validation throws a cryptic `ArgumentException: The value cannot be an empty string (Parameter 'ClientId')` deep in middleware.

### Changes Made

1. **Program.cs**: Added explicit pre-registration guard — reads `Auth0:Domain` and `Auth0:ClientId` from config before calling `AddAuth0WebAppAuthentication`. If either is empty, throws `InvalidOperationException` with clear user-secrets instructions.
2. **appsettings.Development.json**: Added `Auth0` section with empty placeholder values for Domain, ClientId, and ClientSecret — documents what secrets are required.

### Build Validation

- ✅ `dotnet build src/Web/Web.csproj` succeeded with 0 errors, 0 warnings

### Learnings

- Auth0 SDK validates options during `builder.Build()` — validate config *before* registering services to get actionable error messages
- AppHost does NOT inject Auth0 env vars — developers must set user secrets manually on `src/Web`
- `appsettings.Development.json` should document required secret keys (with empty values) so developers know what to configure

## 2026-05-07 — PR #242 Revision: Aragorn Review Blockers (Issue #238)

### Task: Fix three Aragorn-rejected blockers on the light/dark theme toggle PR

**Context:** Aragorn rejected PR #242 (feat(238): fix light/dark theme toggle). Sam owns the revision cycle (Legolas and Gimli locked out per reviewer routing).

### Changes Made

1. **`src/Web/Components/Theme/ThemeProvider.razor.cs`** — Removed ALL `ConfigureAwait(false)` calls.
   - In Blazor Server, component lifecycle methods run on the renderer's synchronization context.
   - `ConfigureAwait(false)` allows continuations to run on thread-pool threads, which means state mutations (`CurrentColor`, `CurrentBrightness`) and `InvokeAsync(StateHasChanged)` can execute off the renderer sync context — unsafe for interactive server components.
   - Fix: plain `await` throughout; let the Blazor runtime keep continuations on the correct context.

2. **`tests/Architecture.Tests/ThemeRenderBoundaryTests.cs`** — Hardened false-pass guard in `RoutesShouldWrapRouterInsideThemeProvider`.
   - The original test only asserted `themeProviderStart >= 0`. If `</Router>` was absent (index = -1), `themeProviderEnd > routerEnd` became `positive > -1 = true` — a false pass.
   - Fix: explicit `BeGreaterThanOrEqualTo(0)` assertions for `routerStart`, `routerEnd`, and `themeProviderEnd` before any positional comparison.

3. **`tests/Web.Tests.Bunit/Components/Theme/ThemeProviderTests.cs`** — Corrected un-awaited `InvokeAsync` calls.
   - Four test methods (`SetColor`/`SetBrightness` × 2) were `void` and discarded the `Task` returned by `cut.InvokeAsync(...)`. Any async failure inside `SetColor`/`SetBrightness` was silently swallowed.
   - Fix: methods converted to `async Task`, `InvokeAsync` awaited.
   - Also added `.SetVoidResult()` to `SetupVoid(...)` handlers — bUnit requires explicit resolution when a handler is explicitly configured, even in `Loose` mode.

### Build Validation

- ✅ Release build: 0 errors, 0 warnings (new code)
- ✅ Architecture.Tests: 15/15 passed
- ✅ Web.Tests.Bunit: 65/65 passed
- ✅ All pre-push gates passed

### Learnings

- **`ConfigureAwait(false)` is never appropriate in Blazor Server component lifecycle methods** — it moves continuations off the renderer sync context, making state mutations and `StateHasChanged` calls racy.
- **Architecture test false-pass pattern**: when index comparisons are used (`IndexOf`), every index value must be individually asserted `>= 0` before positional comparisons; otherwise absent elements (index = -1) can make ordering assertions trivially true.
- **bUnit `SetupVoid` requires `.SetVoidResult()`** when the invocation is actually awaited. In `Loose` mode, un-configured invocations auto-resolve, but explicitly configured handlers (`SetupVoid(...)`) must be resolved with `.SetVoidResult()` or they hang the await.

As part of squad skills/playbooks review, MongoDB DBA patterns + filter pattern identified for formalization.

**Scope:** All `GetAllAsync()` repository methods use `Builders<T>.Filter` pattern; optional params in interface; validation in handlers.

**Action:** Audit all `I*Repository` interfaces + implementations against filter-pattern. Create `.squad/playbooks/repository-query-patterns.md` runbook.

**Collaboration:** With Gimli (Testing) for comprehensive repository layer standardization.

**Timeline:** Sprint 7 (2h estimated).

**Owner:** Sam (Domain Model) — routed with `mongodb-filter-pattern` skill injection.

## 2026-05-08 — Issue #248: AppHost `clear-myblog-data` Real Implementation

### Task

Replace Boromir's tracer-bullet handler in `AppHost.cs` with actual `DeleteManyAsync` logic for the `clear-myblog-data` operator command.

### Changes Made

1. **`src/AppHost/AppHost.cs`** — Replaced stub handler body with full clearing logic:
   - Resolve connection string via `mongo.Resource.ConnectionStringExpression.GetValueAsync(ct)` (see Learnings)
   - Connect `MongoClient`, call `database.ListCollectionNamesAsync()`
   - Skip `system.*` collections
   - `DeleteManyAsync(FilterDefinition<BsonDocument>.Empty)` per collection — documents deleted, collection structure preserved
   - Return structured `ExecuteCommandResult` with per-collection counts and total

### Build Validation

- ✅ `dotnet build src/AppHost/AppHost.csproj` — 0 errors, warnings only (pre-existing CA rules)
- ⚠️ `tests/AppHost.Tests` has pre-existing build errors (Gimli's domain — not touched)

### Learnings

#### Aspire `MongoDBServerResource` Connection String Resolution (Aspire 13.3.0)

- `MongoDBServerResource` does NOT directly expose `GetConnectionStringAsync()` as a callable instance method
- `GetConnectionStringAsync()` is defined as a default interface method on `IResourceWithConnectionString`; calling it requires casting to the interface: `((IResourceWithConnectionString)resource).GetConnectionStringAsync(ct)`
- **Preferred alternative**: use `resource.ConnectionStringExpression.GetValueAsync(ct)` directly — `ConnectionStringExpression` is a `ReferenceExpression` with a public `GetValueAsync(CancellationToken)` method; no cast needed
- The expression resolves to `mongodb://localhost:{allocatedPort}` at runtime after Aspire allocates the endpoint

#### MongoDB.Driver in AppHost — No Extra PackageReference Needed

- `MongoDB.Driver 3.6.0` and `MongoDB.Bson 3.6.0` are compile-time transitive deps via `Aspire.Hosting.MongoDB` → `AspNetCore.HealthChecks.MongoDb`
- Add only `using MongoDB.Bson;` and `using MongoDB.Driver;` — no csproj changes required

#### Full-Collection Wipe Without Dropping

- `collection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty, ct)` deletes all documents while preserving collection structure and indexes
- Issue #248 spec is clear: delete documents, do NOT drop collections
- Boromir's handoff note used the word "drop" loosely — always prefer the issue spec over prose notes

#### Pre-Existing AppHost.Tests Build Errors (Gimli)

- `tests/AppHost.Tests/MongoDbClearCommandTests.cs` does not compile against Aspire 13.3.0:
  - `CustomResourceSnapshot.HealthStatus` and `HealthReports` are read-only (no `init` setter) — object-initializer syntax fails
  - Tests reference command name `"clear-data"` but the actual command is `"clear-myblog-data"`
- These failures are Gimli's responsibility (issue #249)

## 2026-05-xx — Issue #266: Rename `_clearMutex` to `_dbMutex`

### Task

Rename the shared semaphore in `MongoDbResourceBuilderExtensions` from `_clearMutex` to `_dbMutex`;
the field guards all three dev commands (Clear, Seed, Stats), not just Clear.

### Changes Made

- `src/AppHost/MongoDbResourceBuilderExtensions.cs`: updated field comment + renamed 1 declaration + 3 WaitAsync + 3 Release (7 sites)

### Build Validation

- ✅ Build: 0 errors
- ✅ Architecture.Tests: 15/15, Domain.Tests: 42/42, Integration.Tests: 12/12

### PR: #267

## 2026-05-xx — Issue #296: PostAuthor Value Object

### Task

Replace `string Author` on `BlogPost` with an immutable `PostAuthor` value object, per Aragorn's ADR (`aragorn-296-post-author-adr.md`).

### Changes Made

1. **`src/Domain/ValueObjects/PostAuthor.cs`** — New `sealed record PostAuthor(string Id, string Name, string Email, IReadOnlyList<string> Roles)` with `PostAuthor.Empty` helper
2. **`src/Domain/Entities/BlogPost.cs`** — `Author` property changed from `string` to `PostAuthor`; `Create()` guards: `ArgumentNullException.ThrowIfNull(author)` then `ArgumentException.ThrowIfNullOrWhiteSpace(author.Name)`
3. **`src/Web/Data/BlogDbContext.cs`** — Added `entity.OwnsOne(p => p.Author, ...)` with `HasElementName` for each field to control MongoDB sub-document field names
4. **`src/Web/Data/BlogPostDto.cs`** — Replaced `string Author` with `string AuthorId, string AuthorName, string AuthorEmail, IReadOnlyList<string> AuthorRoles` (positional record — all call sites updated)
5. **`src/Web/Data/BlogPostMappings.cs`** — Updated `ToDto()` to map flat author fields
6. **`src/Web/Features/BlogPosts/Create/CreateBlogPostCommand.cs`** — `PostAuthor Author` replaces `string Author`
7. **`src/Web/Features/BlogPosts/Create/CreateBlogPostCommandValidator.cs`** — `NotNull()` on Author + `NotEmpty().When(author not null)` on Author.Name
8. **`src/Web/Features/BlogPosts/Create/Create.razor`** — Temporary stub builds `PostAuthor` from `_model.Author` string field; Legolas must replace with `AuthenticationStateProvider` injection
9. **`src/Web/Features/BlogPosts/List/Index.razor`** — `@post.Author` → `@post.AuthorName`
10. **All test projects** — GlobalUsings, entity construction, DTO construction updated

### Build Validation

- ✅ Release build: 0 errors, 0 warnings
- ✅ Architecture.Tests: 16/16
- ✅ Domain.Tests: 42/42
- ✅ Web.Tests: 151/151
- ✅ Integration.Tests: 12/12 (Docker)
- ✅ All pre-push gates passed

### PR: #298

### Learnings

#### `PostAuthor.Empty` is a testing convenience — not valid for `BlogPost.Create()`

- `PostAuthor.Empty` has `Name = ""`. The `BlogPost.Create()` guard rejects empty Name.
- Tests that exercise handler behavior (not null/empty author validation) must use `new PostAuthor("", "Test Author", "", [])`, not `PostAuthor.Empty`.
- Only use `PostAuthor.Empty` in command/validator tests that specifically test the "empty author" failure path.

#### Razor files need explicit `@using` for value objects

- Razor components do not automatically inherit global usings from `GlobalUsings.cs` files in the same project for all scenarios.
- Add `@using MyBlog.Domain.ValueObjects` explicitly at the top of any `.razor` file that references `PostAuthor`.

#### Breaking schema changes need a migration note in the PR

- MongoDB `OwnsOne` with `HasElementName` changes the stored field name from a flat `"Author"` string to a sub-document `{"AuthorId":...,"AuthorName":...}`.
- Existing documents fail to deserialize; always document the migration strategy (drop/recreate in dev; script in prod) in the PR.

## 2026-06-11 — Issue #300: Audit — Restrict Blog Post Editing to Author or Admin

### Task

Audit and confirm correctness of the backend server-side authorization
for issue #300 (restrict blog post editing to post author or Admin).

### Findings

The backend implementation was already complete in commit `ee0aafb`:

1. **`EditBlogPostCommand`** carries `CallerUserId` (Auth0 sub claim) and `CallerIsAdmin`.
2. **`EditBlogPostHandler`** enforces the rule before calling `repo.UpdateAsync`:

   ```csharp
   if (!request.CallerIsAdmin && post.Author.Id != request.CallerUserId)
       return Result.Fail("You are not authorized to edit this post.", ResultErrorCode.Unauthorized);
   ```

3. **`tests/Web.Tests/Handlers/EditBlogPostHandlerTests.cs`** covers all three
   scenarios: author edits own post (success), admin edits any post (success),
   non-admin non-author returns `ResultErrorCode.Unauthorized`.
4. All 154 `Web.Tests` pass.

### Authorization placement decision

The check belongs in the **handler**, not in the FluentValidation validator,
because the rule requires the persisted `post.Author.Id` — unavailable at
validation time. Putting it in the handler makes it impossible to bypass via
alternative command dispatchers.

### Inbox notes written

- `.squad/decisions/inbox/sam-issue300-authorization.md`
- `.squad/decisions/inbox/sam-issue300-tests.md`

### Learnings

#### Handler is the correct home for post-load authorization checks

- If an authorization rule depends on persisted entity state (e.g., "does the
  caller own this record?"), enforce it inside the handler after loading the
  entity — not in a validator, not in the UI layer.
- This keeps the rule on every code path and keeps validators focused on
  data shape.

#### `CallerUserId` should NOT be validated in FluentValidation

- An empty `CallerUserId` with `CallerIsAdmin = false` will still fail the
  handler's auth check (`"" != post.Author.Id`), so no separate validator rule
  is required.
- Only validate command properties that can be wrong regardless of persisted state.

#### Test files written with implementation commit are still Gimli's long-term property

- For atomic commits, writing handler tests alongside the implementation is
  acceptable. Note the crossing in an inbox file so Gimli can audit or extend
  coverage as needed.

## 2026-06-xx — dotnet-version-upgrade: Pre-Push Build Validation

### Task

Verify the `.csproj` and `global.json` changes on the `dotnet-version-upgrade` branch build correctly before opening a PR.

### Findings & Fixes

1. **`ServiceDefaults.csproj` and `Web.Tests.Integration.csproj` were 0-byte** — the upgrade commit accidentally zeroed them out. Restored both from git history (`git show 0c8e6af:...`).
2. **`<GenerateDocumentationFile>true</GenerateDocumentationFile>` was accidentally added to `Web.csproj`** — this was not present pre-upgrade and caused 12 CS1591 "Missing XML comment" errors on public Blazor/EF types (`ThemeProvider`, `BlogDbContext`, `partial class Program`). Removed the setting; Web is an application, not a library.
3. **`dotnet test` with multiple project paths on Windows** — `dotnet test tests\A tests\B` fails with MSB1008; must run each `.csproj` separately.

### Build Validation

- ✅ `dotnet restore` — clean, 0 errors
- ✅ `dotnet build -c Release` — 0 errors (3 pre-existing CA1848 warnings in AppHost)
- ✅ Domain.Tests: 42/42
- ✅ Architecture.Tests: 16/16
- ✅ Web.Tests: 165/165
- ✅ Web.Tests.Bunit: 94/94

### Commit

`fix(build): restore zeroed csproj files and remove accidental GenerateDocumentationFile`

## Learnings

### Zeroed csproj files during version upgrade

- Git-based upgrade scripts or search-replace tooling can accidentally zero out `.csproj` files. Always verify file sizes after an upgrade commit with `Get-Item *.csproj | Select Length`.
- Recovery: `git show <pre-upgrade-sha>:"path/to/file.csproj"` restores the original content.

### `GenerateDocumentationFile` belongs on libraries, not application projects

- Adding `<GenerateDocumentationFile>true</GenerateDocumentationFile>` to an application `.csproj` with `TreatWarningsAsErrors>true` will cause CS1591 errors for every public type that lacks XML docs.
- Application projects (Blazor apps, API hosts) should never have this setting; only library/package projects need it.

### `dotnet test` on Windows does not support multiple project paths

- `dotnet test tests\A tests\B` fails with MSB1008 "only one project can be specified" on Windows.
- Run tests one `.csproj` at a time, or use the solution file: `dotnet test MyBlog.slnx -c Release`.

### `_isLoading` field initializers only run at construction — not on reuse

- Blazor Server components are reused across navigation when the router keeps the same component instance (same page, different route params). `OnParametersSetAsync` fires but the constructor does not.
- Any UI state that should reset per parameter change (`_isLoading`, `_error`, `_concurrencyError`) must be explicitly reset at the **top** of `OnParametersSetAsync`, before any `try` block.
- Pattern: `_isLoading = true;` as the first statement ensures every fetch — initial and reuse — shows the loading indicator and hides stale content.

### bUnit cannot easily assert intermediate async state

- In bUnit 2.x, `IRenderedComponent<TComponent>` does NOT have `SetParametersAndRender`. The correct method for re-rendering with new parameters is `cut.Render(parameters => ...)` (extension from `RenderedComponentRenderExtensions`).
- `cut.Render(...)` waits for the full async lifecycle to complete before returning. There is no built-in way to pause at `_isLoading = true` and then check markup.
- The practical test strategy for parameter-change behavior: verify the **final state** shows the new data (not stale old data). This proves the lifecycle ran correctly without requiring thread hacks.

### PR #309 review findings (Aragorn)

- Always check whether a boolean flag initialized at field level is ever *reset* in `OnParametersSetAsync` before the async work begins.
- File: `src/Web/Features/BlogPosts/Edit/Edit.razor`
- Test: `tests/Web.Tests.Bunit/Features/EditAclTests.cs` → `EditShowsNewPostContentAfterParameterChange`

## Issue #339 Category CRUD — Backend Implementation (2026-05-15)

Implemented full backend slice for Category CRUD: entity, repository, MediatR handlers, validators with unique name enforcement, safe-delete guard.
Added nullable CategoryId FK to BlogPost with domain methods AssignCategory/RemoveCategory.
Seeded default "General" category with stable Guid. All handlers follow existing Result pattern.
Backend complete; Gimli's tests passing; decision documented in decisions/inbox.

## PR #338 Skill Template Compliance (2026-05-15)

Fixed three blockers in `.squad/skills/self-authored-pr-gate/SKILL.md`: added `domain` and `source: "earned"` YAML fields per repo skill template,
clarified Codecov gating language from "hard block" to "investigate-and-explain" to align with squad practice,
fixed related heading hierarchy. Minimal two-line change preserving intent while improving alignment with squad conventions.

## Issue #341 Seed Log Wording Fix (2026-05-15)

Corrected three log strings in `MongoDbResourceBuilderExtensions.cs` to match the actual upsert behavior of the General category seed.
The category is seeded via `ReplaceOneAsync` with `IsUpsert=true`, so "inserted" was inaccurate.
Changed to "upserted" in the invocation log, completion log, and result message string.
Blog posts (seeded via `InsertManyAsync`) correctly retain "inserted".
AppHost.Tests: 48/48 passed. Scope: log wording only; no logic changes.

### Learning: Log wording must match the actual DB operation semantics

When upsert (`ReplaceOneAsync + IsUpsert=true`) is the behavior, log strings must say "upserted" or "inserted/updated" — not "inserted". Future seed operations: always audit log strings against the actual driver call used.

---

## Issue #345 History Correction (2026-05-17)

**By:** Scribe  
**Reason:** Reviewer lockout required an independent correction to Sam's history
artifact, so the stale guidance was revised in place instead of being left as an
active recommendation.

### Canonical Outcome

- Use MongoDB 7 in AppHost for this fix path.
- Use `mongo-data-v7` for the persistent volume.
- Treat MongoDB 8.x as the affected family in this environment because those
  images require AVX-capable CPUs and crash before the app can connect.
- Do not reuse the legacy `mongo-data` volume after a MongoDB 8.x run; it can
  retain feature compatibility version metadata that makes MongoDB 7 exit with
  code 62.

### Validation Results

- ✅ Build: `dotnet build MyBlog.slnx -c Release --no-restore` passed
- ✅ Format: `dotnet format MyBlog.slnx --verify-no-changes --no-restore` passed
- ✅ Regression tests: `dotnet test tests/AppHost.Tests -c Release --no-restore --filter 'FullyQualifiedName~MongoDbContainerConfigurationTests'` — 4/4 passed
- ✅ Smoke test: `aspire start --isolated --apphost src/AppHost/AppHost.csproj` — MongoDB healthy on `docker.io/library/mongo:7` with volume `mongo-data-v7`; all services running

### Standing Rule (From Decision)

When MongoDB major version changes on an Aspire dev environment with a
pre-existing persistent volume, suffix the volume name with `v{major}` (for
example, `mongo-data-v7`). This prevents feature compatibility version mismatch
crashes transparently.

---

## 2026-05-23 — Issue #350 Session Closeout (Orchestration Coordination)

### Session Summary

Sam completed two parallel tracks for Issue #350:

1. **Runtime wiring verification** — Confirmed all AppHost, ServiceDefaults, and Web startup code correct; identified and fixed MSBuild npm-install gap
2. **Analyzer warning cleanup** — Resolved 37 pre-existing warnings in `MongoDbResourceBuilderExtensions.cs` (CA2007, CA1848, CA1873, CA1305)

Both tracks shipped with zero test failures:

- Web.Tests: 210/210 passed
- Architecture.Tests: 16/16 passed

### Decisions Recorded

- Decision 4: Auto-run npm install in MSBuild when node_modules is missing
- Decision 6: Resolve analyzer warnings in MongoDbResourceBuilderExtensions.cs

### Files Changed

- `src/Web/Web.csproj` — Added `EnsureNpmPackages` MSBuild target
- `src/AppHost/MongoDbResourceBuilderExtensions.cs` — Refactored logging to source-generated delegates, added `.ConfigureAwait(false)`

### Status

✅ Completed. Open gates (AppHost.Tests startup failures) assigned to Boromir/Gimli follow-up agents.
