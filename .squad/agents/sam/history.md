# Sam's Work History

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
