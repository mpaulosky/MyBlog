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

## 2026-04-19 — MongoDB Query Patterns Adoption (Skills Review)

As part of squad skills/playbooks review, MongoDB DBA patterns + filter pattern identified for formalization.

**Scope:** All `GetAllAsync()` repository methods use `Builders<T>.Filter` pattern; optional params in interface; validation in handlers.

**Action:** Audit all `I*Repository` interfaces + implementations against filter-pattern. Create `.squad/playbooks/repository-query-patterns.md` runbook.

**Collaboration:** With Gimli (Testing) for comprehensive repository layer standardization.

**Timeline:** Sprint 7 (2h estimated).

**Owner:** Sam (Domain Model) — routed with `mongodb-filter-pattern` skill injection.

## 2026-05-07 — PR #243 Revision: Aragorn's Blockers Fixed

### Task: Fix three test blockers flagged by Aragorn on PR #243 (issue #239)

Aragorn rejected PR #243 with three specific blockers. Legolas and Gimli locked out per lockout rule. Sam owns the full revision cycle.

### Blockers Addressed

**1. ThemeLayerTests.cs — tautological layout namespace test**

`LayoutComponents_ShouldResideIn_LayoutNamespace` filtered types by namespace and then asserted the same namespace — a tautology that provided zero architectural protection. Replaced with `ThemeComponents_ShouldNotDependOn_LayoutComponents`, which enforces the one-way coupling rule: layout components may consume theme components, but theme components must never depend on layout.

**2. ThemeColorPersistenceTests.cs — async write path not properly awaited**

Three test methods (`ThemeProviderSetColor_WritesToJs_*`, `ThemeProviderSetBrightness_WritesToJs_*`, `ThemeProvider_AfterSetColorRed_*`) were `void` and called `cut.InvokeAsync(...)` without `await`. Promoted all three to `async Task` and awaited the `InvokeAsync` call so the full `InvokeVoidAsync` write path completes before assertions run.

**3. ThemeColorPersistenceTests.cs — JS-failure catch branch uncovered**

Added two new tests:

- `ThemeProvider_SetColor_SilentlySwallowsJsException_AndKeepsNewColor`
- `ThemeProvider_SetBrightness_SilentlySwallowsJsException_AndKeepsNewBrightness`

Both use `JSRuntimeMode.Strict` with the respective write method (`setColor`/`setBrightness`) **not** planned.
bUnit throws on the unplanned `InvokeVoidAsync` call, exercising the catch branch.
Tests assert that `CurrentColor`/`CurrentBrightness` is updated and no exception propagates to the caller.
No production code change required — the bare `catch {}` blocks in `ThemeProvider.razor.cs` are already correct.

### Build & Test Results

- Build: 0 errors, 46 pre-existing test-project warnings (CA1707/CA1307 naming — not introduced by this change)
- Architecture.Tests: 13/13 passed
- Web.Tests.Bunit: 89/89 passed (up from 87 before JS-failure tests were added)
- Domain.Tests: 42/42 passed
- Web.Tests: 127/127 passed
- All pre-push gates passed; pushed to `squad/239-fix-theme-color-selector-persistence`

### Learnings

- **Tautological architecture tests are silently useless.** `ResideInNamespace("X").Should().ResideInNamespace("X")` always passes and provides no enforcement. The meaningful pattern is `.ShouldNot().HaveDependencyOn(...)`.
- **bUnit `InvokeAsync` must be awaited in async tests.** Fire-and-forget `InvokeAsync` in a `void` test can pass intermittently if `WaitForAssertion` retries long enough, but it doesn't pin the full async path. Always `await cut.InvokeAsync(...)` in `async Task` tests.
- **`JSRuntimeMode.Strict` is the correct tool for catch-branch coverage.** Setting Strict mode and leaving a specific invocation unplanned causes bUnit to throw a `JSException`-derived exception on `InvokeVoidAsync`. The production `catch {}` swallows it. This cleanly exercises the error path without mocking custom exception types.
