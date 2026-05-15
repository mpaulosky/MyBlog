# Sam's Work History

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
