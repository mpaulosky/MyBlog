---
# Transactions and Concurrency Handling

**Date:** 2025-01-29  
**Architect:** Aragorn  
**Status:** Proposed  
**Context:** Training project using MongoDB via EF Core adapter with Vertical Slice Architecture

---

## Question 1: Should We Use MongoDB Transactions?

### Current State
- MongoDB accessed via `MongoDB.EntityFrameworkCore` adapter
- Single-document operations only (CRUD on BlogPost entities)
- `SaveChangesAsync()` calls are atomic at the document level (MongoDB guarantee)
- No multi-document operations or complex workflows
- Aspire local MongoDB container (standard standalone instance, **not** replica set)

### Analysis
**MongoDB transactions require a replica set.** The Aspire `.AddMongoDB("mongodb")` provisioning creates a standalone MongoDB instance, which does NOT support multi-document transactions.

**Do we need transactions for this project?**
- Current operations are single-document CRUD → inherently atomic
- No cross-document consistency requirements
- No batch operations spanning multiple documents
- Blog post operations are independent (create/update/delete one post at a time)

**Cost/Complexity vs. Benefit:**
- **To enable transactions:** Would require configuring MongoDB replica set (3+ nodes), either locally via Docker Compose or migrating to MongoDB Atlas. This adds significant infrastructure complexity.
- **Benefit:** Zero — we have no multi-document operations that require transactional atomicity.

### Decision: **NO — Do not implement transactions**

**Reasoning:**
1. MongoDB's single-document operations are already atomic
2. We have no multi-document workflows requiring transactions
3. Local dev environment doesn't support replica sets without major config changes
4. This is a training project — complexity doesn't justify theoretical future-proofing
5. If multi-document atomicity becomes required later, we can revisit

**Action:** Document this decision and move forward without transactions.

---

## Question 2: How Are We Handling Concurrency?

### Current State
**We are NOT handling concurrency at all.** Here's what I found:

1. **No concurrency tokens:** `BlogPost` entity has no version field, timestamp, or `[ConcurrencyCheck]` attribute
2. **No EF Core concurrency:** `BlogDbContext` has no concurrency configuration
3. **No exception handling:** Handlers don't catch `DbUpdateConcurrencyException`
4. **Result pattern ready:** `ResultErrorCode.Concurrency = 1` exists but is unused
5. **Last-write-wins:** If two admins edit the same post simultaneously, the last `SaveChangesAsync()` silently overwrites the first

### Concurrency Scenarios for Blog CRUD
**Realistic conflict scenarios:**
- Two admins editing the same blog post simultaneously
- Admin editing while another admin deletes the same post
- Race condition: Admin updates post while cached read serves stale data

**Risk level for training project:** Low-to-Medium
- Single-user dev environment → conflicts unlikely
- Production blog with multiple admins → conflicts possible but rare
- Impact: Lost edits (frustrating but not data-corrupting)

### Implementation Options

#### Option A: **Optimistic Concurrency (Recommended)**
Add a version field to `BlogPost`:

```csharp
// BlogPost.cs
public int Version { get; private set; }

// Increment on update
public void Update(string title, string content)
{
    Title = title;
    Content = content;
    UpdatedAt = DateTime.UtcNow;
    Version++; // Increment version
}
```

Configure in `BlogDbContext`:
```csharp
entity.Property(p => p.Version).IsConcurrencyToken();
```

Update handlers to catch and handle:
```csharp
catch (DbUpdateConcurrencyException ex)
{
    return Result.Fail("Post was modified by another user. Please reload and try again.",
        ResultErrorCode.Concurrency,
        new { ServerVersion = ex.Entries.First().GetDatabaseValues()?["Version"] });
}
```

**Pros:**
- Industry-standard pattern for web apps
- MongoDB EF Core provider supports `IsConcurrencyToken()`
- Teaches proper concurrency handling
- Minimal performance impact (one field)
- Fails fast with clear error message

**Cons:**
- Requires schema change (add Version field)
- Requires handler updates
- Requires UI to handle concurrency errors (reload + retry)

#### Option B: **Do Nothing (Acceptable for Training)**
Accept last-write-wins behavior.

**Pros:**
- Zero code changes
- Zero complexity
- Acceptable for single-developer training scenario

**Cons:**
- Silent data loss if conflicts occur
- Doesn't teach proper patterns
- Poor user experience if multiple admins exist

#### Option C: **Pessimistic Locking (Overkill)**
Use MongoDB transactions with `FindOneAndUpdate` locking.

**Pros:**
- Prevents conflicts entirely

**Cons:**
- Requires replica set (see Question 1)
- Over-engineered for blog CRUD
- Reduces throughput

---

## Recommendation

### For This Training Project: **Implement Optimistic Concurrency (Option A)**

**Rationale:**
1. **Pedagogical value:** Teaches industry-standard pattern (optimistic concurrency)
2. **Minimal complexity:** Single field + exception handling
3. **Real-world readiness:** Pattern scales to production scenarios
4. **Respects user edits:** Prevents silent data loss
5. **Already prepared:** `ResultErrorCode.Concurrency` exists

### Implementation Plan
If team approves, I will:

1. **Domain layer:**
   - Add `public int Version { get; private set; }` to `BlogPost`
   - Increment `Version++` in `Update()` method

2. **Data layer:**
   - Configure `.IsConcurrencyToken()` in `BlogDbContext.OnModelCreating()`

3. **Handler layer:**
   - Wrap `SaveChangesAsync()` calls in `try-catch` for `DbUpdateConcurrencyException`
   - Return `Result.Fail(..., ResultErrorCode.Concurrency, details)` with server version

4. **UI layer:**
   - Display concurrency error message
   - Prompt user to reload and retry
   - (Future enhancement: Show diff of conflicting changes)

5. **Testing:**
   - Write integration test simulating concurrent edits

**Estimated effort:** 2-4 hours  
**Risk:** Low (well-understood pattern)

---

## Summary

| Question | Answer | Action |
|----------|--------|--------|
| **Transactions?** | No — not needed for single-document CRUD | Document and close |
| **Concurrency?** | Currently unhandled (last-write-wins) | Implement optimistic concurrency |

**Next Steps:**
1. Team review this decision
2. If approved, create implementation tasks
3. Update handlers, entity, and context
4. Add integration tests

---

**Reviewed by:** _(pending)_  
**Approved by:** _(pending)_

---
### 2026-04-17T15:30: User directive
**By:** Matthew Paulosky (via Copilot)
**What:** Do not prefix project folders or .csproj file names with `MyBlog.` — the repo name already provides that context. Namespaces may still use `MyBlog.*` via `<RootNamespace>`.
**Why:** User request — captured for team memory

---
### 2026-04-17T15:30: User directive
**By:** Matthew Paulosky (via Copilot)
**What:** This is a training project only — not production. Do not apply production-level concerns (e.g., hardened security, HA, SLAs) unless explicitly asked.
**Why:** User request — captured for team memory

---
### 2026-04-17: User directive
**By:** mpaulosky (via Copilot)
**What:** Do not prefix project/folder names with the repo name. Projects live under `src/AppHost`, `src/Web`, `src/Domain` — not `src/MyBlog.AppHost` etc. The repo context is implicit. Namespaces may still use the `MyBlog.*` root namespace for clarity.
**Why:** User request — captured for team memory

---
# Decision: VSA Handler Test Coverage Complete

**Date**: 2025  
**Author**: Gimli (Tester)  
**Status**: Accepted

## Context

MyBlog was upgraded to Vertical Slice Architecture (VSA) with MediatR handlers, MongoDB, Redis/IDistributedCache, IMemoryCache (L1), and Auth0. Existing tests covered domain entities only. Handler-level unit tests were missing.

## Decision

Write comprehensive unit tests for all 4 CRUD handler groups using xUnit + FluentAssertions + NSubstitute. Architecture tests extended to enforce VSA structural constraints.

## Test Files Created

| File | Tests | Status |
|------|-------|--------|
| `Handlers/GetBlogPostsHandlerTests.cs` | 4 | ✅ Pass |
| `Handlers/CreateBlogPostHandlerTests.cs` | 2 | ✅ Pass |
| `Handlers/DeleteBlogPostHandlerTests.cs` | 2 | ✅ Pass |
| `Handlers/EditBlogPostHandlerTests.cs` | 5 | ✅ Pass |
| `Architecture.Tests/VsaLayerTests.cs` | 3 | ✅ Pass |

**Total**: 20 unit tests + 6 architecture tests = 26 tests, 0 failures.

## Constraints Applied

- `IMemoryCache.Set<T>` is an extension method — mock `CreateEntry` instead.
- `IMemoryCache.TryGetValue` out-param: use `Returns(x => { x[1] = value; return true; })`.
- NetArchTest 1.3.2 has no `.Exist()` condition — use `GetTypes().Should().BeEmpty()`.
- No explicit `Microsoft.Extensions.Caching.*` package refs in Unit.Tests.csproj (causes NU1605); types come transitively via Web project reference.

## Consequences

- All handler business logic paths (cache hit L1, cache hit L2, cache miss, not found, exception) are tested.
- Architecture tests prevent re-introduction of InMemory repositories and enforce handler sealing and feature isolation.

---
### 2025-04-17: Project is MyBlog training blog app

**By:** Pippin (Docs)

**What:** MyBlog is a training/learning project to practice .NET Aspire orchestration, Blazor Server rendering, and clean architecture. Stack: .NET 10, Aspire 13.2.2, Blazor Server (Interactive Server Rendering), in-memory repository only. No database, no auth, no cache. Domain model: BlogPost entity with factory method and mutation methods. Clean architecture with Domain/Web layering. 9 tests total: 7 unit tests (BlogPost, InMemoryBlogPostRepository), 2 architecture tests (NetArchTest.Rules). All tests passing.

**Why:** User (Matthew Paulosky) directive — this is a hands-on learning project, not production. Captured for team memory to ensure all documentation and decisions stay scoped to actual project state.

**Implications:**
- Documentation always reflects training focus (no production concerns)
- Short project names (no MyBlog. prefix) — repo name provides context
- No external services (Auth0, MongoDB, Redis) — by design
- InMemoryBlogPostRepository is intentional (training)
- Tests are core to learning (TDD emphasis)

---
## Additional Decisions (Merged from Inbox)


### 2025-01-23T18:30:00Z: Optimistic Concurrency — Domain Layer
**By:** Aragorn
**What:** Added Version field to BlogPost entity (private setter, incremented in Update()). Configured IsConcurrencyToken() in BlogDbContext.OnModelCreating for MongoDB EF Core optimistic concurrency.
**Why:** Training project demonstrating the pattern. ResultErrorCode.Concurrency was pre-existing but unused.

# Tailwind Migration Skill Review

**Reviewer:** Aragorn (Lead Architect)  
**Date:** 2025-07-14  
**Skill File:** `/home/mpaulosky/.config/squad/.github/skills/tailwind-migration/SKILL.md`  
**Project:** MyBlog — Blazor Server, Vertical Slice Architecture, `src/` root

---

## Findings

---

### F-01 — Wrong Project Paths Throughout
- **Severity:** 🔴 Critical
- **Area:** 4 — Path / Project Structure Accuracy
- **Finding:** Every path in the skill assumes files live at `./Web/` (e.g., `./Web/wwwroot/app.css`, `./Web/Components/**`). The actual project structure places everything under `./src/Web/` (confirmed: `/home/mpaulosky/Repos/MyBlog/src/Web/`). The `package.json` npm scripts will fail, the `tailwind.config.js` content glob will scan nothing, and the MSBuild `WorkingDirectory` will be wrong.
- **Recommendation:** Replace all `./Web/` references with `./src/Web/`. Specifically:
  - `package.json` scripts: `tailwindcss -i ./src/Web/wwwroot/app.css -o ./src/Web/wwwroot/tailwind.css`
  - `tailwind.config.js` content: `'./src/Web/**/*.{razor,html,cshtml,cs}'`
  - MSBuild `WorkingDirectory`: use `$(SolutionDir)` which resolves to the repo root (correct if `package.json` is at root)

---

### F-02 — VSA Pages: Wrong Page List
- **Severity:** 🔴 Critical
- **Area:** 5 — VSA Project Structure
- **Finding:** Step 6 lists `Home.razor`, `Counter.razor`, `Weather.razor`, `Error.razor`, `NotFound.razor` — all from the default Blazor template. This project's actual pages live under `src/Web/Features/` in VSA slices:
  - `BlogPosts/List/Index.razor`
  - `BlogPosts/Create/Create.razor`
  - `BlogPosts/Edit/Edit.razor`
  - `BlogPosts/Delete/ConfirmDeleteDialog.razor`
  - `UserManagement/ManageRoles.razor`
  - (plus auth-gated routes)
- **Recommendation:** Remove the hardcoded page table. Replace Step 6 with generic guidance: "Apply Tailwind utilities to all `.razor` files found under `src/Web/Features/` and `src/Web/Components/`. Focus on form layouts, table/list views, dialog modals, and role-gated panels." Add a note that `Counter.razor` and `Weather.razor` do not exist in this project.

---

### F-03 — Tailwind.config.js Content Glob Misses Features Directory
- **Severity:** 🔴 Critical
- **Area:** 9 — Tailwind Configuration Quality
- **Finding:** The `content` glob is `'./Web/Components/**/*.{razor,html}'`. This misses:
  - `src/Web/Features/**` (all VSA slice pages)
  - `*.cshtml` (Razor Pages, if any)
  - `*.cs` (C# files where Tailwind class strings might appear)
  - The path prefix is wrong (see F-01)
- **Recommendation:**
  ```js
  content: [
    './src/Web/**/*.{razor,html,cshtml,cs}',
  ],
  ```

---

### F-04 — Dynamic Theme Classes Not Safelisted
- **Severity:** 🔴 Critical
- **Area:** 9 — Tailwind Configuration Quality
- **Finding:** Theme classes (`theme-red`, `theme-blue`, `theme-green`, `theme-yellow`) are applied at runtime via `document.body.classList.add(t)` from JavaScript. Tailwind's JIT scanner won't find these class names in source files, so they will be purged from the compiled CSS. The theme switcher will be silently broken in production builds.
- **Recommendation:** Add a safelist to `tailwind.config.js`:
  ```js
  safelist: [
    'theme-red',
    'theme-blue',
    'theme-green',
    'theme-yellow',
    'dark',
  ],
  ```

---

### F-05 — Bootstrap JS Not Fully Removed
- **Severity:** 🔴 Critical
- **Area:** 2 — Completeness of Bootstrap Removal
- **Finding:** Step 8 mentions removing `bootstrap.bundle.min.js` from `App.razor`, but the actual `App.razor` does **not** include a `<script>` tag for it — Bootstrap JS is delivered as a static file in `wwwroot/lib/bootstrap/dist/js/` (confirmed: `bootstrap.bundle.min.js`, `bootstrap.bundle.js`, etc. are present). The skill's Step 8 only says "Delete or empty `Web/wwwroot/lib/bootstrap/`" but the step that removes the CSS link from `App.razor` (Step 2f) doesn't cross-reference deleting the lib directory. The current `App.razor` references Bootstrap CSS via `@Assets["lib/bootstrap/dist/css/bootstrap.min.css"]` — this asset fingerprinting syntax isn't addressed.
- **Recommendation:** 
  1. In Step 2f, explicitly show removing the `@Assets["lib/bootstrap/dist/css/bootstrap.min.css"]` link (not a plain href).
  2. In Step 8, confirm deletion of `src/Web/wwwroot/lib/bootstrap/` covers both CSS and JS artifacts.
  3. Note that no Bootstrap JS `<script>` tag appears in `App.razor`, so no script removal is needed from that file — but the physical files must still be deleted to reduce bundle size.

---

### F-06 — Blazor Form Validation Classes Not Handled
- **Severity:** 🔴 Critical
- **Area:** 3 — Blazor-Specific Concerns
- **Finding:** The skill has no mention of Blazor's auto-generated validation CSS classes: `.valid`, `.invalid`, `.modified`, `.validation-message`. Bootstrap's CSS styled these automatically. After migration, `EditForm` components (used in `Create.razor` and `Edit.razor`) will emit these classes with zero styling — form validation feedback becomes invisible.
- **Recommendation:** Add a Step (between current Steps 3 and 4) to include Blazor validation CSS in `app.css`:
  ```css
  @layer components {
    .valid.modified:not([type=checkbox]) { @apply border-green-500; }
    .invalid { @apply border-red-500; }
    .validation-message { @apply text-red-600 text-sm mt-1; }
  }
  ```

---

### F-07 — Reference Files Don't Exist
- **Severity:** 🔴 Critical
- **Area:** 8 — Reference File Strategy
- **Finding:** Steps 3, 4, 5, and 6 repeatedly reference files like `./references/app.css`, `./references/MainLayout.razor`, `./references/NavMenu.razor`, `./references/pages/`. These files do not exist in the skill directory (only `SKILL.md` exists). An agent executing this skill will be blocked at Step 3 with no content to write.
- **Recommendation:** Either:
  - **Option A (Preferred):** Inline all reference file contents directly in `SKILL.md` as fenced code blocks within each step. This makes the skill self-contained.
  - **Option B:** Create the reference files alongside `SKILL.md` (requires restructuring the skill directory).
  The current state makes the skill non-executable for any AI agent.

---

### F-08 — Tailwind v3 vs v4 Ambiguity
- **Severity:** 🟡 Important
- **Area:** 1 — Tailwind Version Accuracy
- **Finding:** The skill description says "Tailwind CSS v4+" but `package.json` pins `"tailwindcss": "^3.4.0"`. Tailwind v4 uses a completely different setup: no `tailwind.config.js`, CSS-first config via `@import "tailwindcss"` in the CSS file, different CLI, and different content scanning. These two approaches are incompatible. Using `^3.4.0` will install v3 while the description misleads agents into thinking v4 behavior applies.
- **Recommendation:** Choose one and be explicit. For maximum stability, pin v3:
  - Change description to "Tailwind CSS v3"
  - Pin `"tailwindcss": "^3.4.17"` (latest v3)
  - Document v4 migration as a future step if desired
  OR commit fully to v4 and rewrite the entire setup section (removing `tailwind.config.js`, changing directives to `@import "tailwindcss"`, updating CLI flags).

---

### F-09 — MSBuild Target Will Break CI Without npm
- **Severity:** 🟡 Important
- **Area:** 6 — MSBuild Integration
- **Finding:** The `<Exec Command="npm run tw:build" />` MSBuild target will cause `dotnet build` to fail in CI/CD environments where npm is not installed (e.g., standard `mcr.microsoft.com/dotnet/sdk` Docker images). There is no `ContinueOnError` or conditional guard.
- **Recommendation:** Add a condition to skip on CI if npm is absent, or use `dotnet-tailwind` NuGet package as a more .NET-native approach. At minimum add:
  ```xml
  <Target Name="BuildTailwind" BeforeTargets="Build" Condition="'$(CI)' != 'true'">
    <Exec Command="npm run tw:build" WorkingDirectory="$(SolutionDir)" />
  </Target>
  ```
  Or prefer the `Tailwind.Extensions.AspNetCore` / `dotnet-tailwind` tool approach which keeps the toolchain .NET-native.

---

### F-10 — NavMenu Has Auth-Gated Links Not in Skill Template
- **Severity:** 🟡 Important
- **Area:** 5 — VSA Project Structure
- **Finding:** The actual `NavMenu.razor` includes `<AuthorizeView Roles="Author,Admin">` and `<AuthorizeView Roles="Admin">` blocks wrapping "New Post" and "Manage Users" links, plus a login/logout toggle. The skill's generic NavMenu template doesn't mention preserving `<AuthorizeView>` wrappers. An agent rewriting NavMenu from the template could drop role-based access control.
- **Recommendation:** Step 5 must explicitly state: "Preserve all `<AuthorizeView>` blocks from the original `NavMenu.razor`. Wrap nav links for Blog Create, Blog Edit, Blog Delete, and User Management in their original role guards. The top nav bar must include login/logout using the same auth pattern."

---

### F-11 — ReconnectModal.razor Not Addressed
- **Severity:** 🟡 Important
- **Area:** 3 — Blazor-Specific Concerns
- **Finding:** The project has a `ReconnectModal.razor` and `ReconnectModal.razor.css` with Bootstrap-dependent styling. This custom component is not mentioned in the skill's file modification table or Step 8 cleanup. After migration, the reconnect modal will render unstyled or broken.
- **Recommendation:** Add to the "Files to Create / Modify" table:
  | `Web/Components/Layout/ReconnectModal.razor` | Update — replace Bootstrap classes with Tailwind |
  | `Web/Components/Layout/ReconnectModal.razor.css` | Delete or migrate styles to Tailwind utilities |

---

### F-12 — `.gitignore` Not Updated
- **Severity:** 🟡 Important
- **Area:** 7 — Missing Steps
- **Finding:** After adding `node_modules/` (from `npm install`) and generating `tailwind.css` (compiled output), the `.gitignore` should be updated. The skill has no step for this. Committing `node_modules/` or the compiled CSS to git are both common mistakes.
- **Recommendation:** Add a step after Step 2b:
  ```
  Add to .gitignore:
  node_modules/
  # Either exclude the compiled output (regenerate on build):
  src/Web/wwwroot/tailwind.css
  # OR include it (pre-built for deployment without npm):
  # !src/Web/wwwroot/tailwind.css
  ```
  Document the trade-off: excluding requires npm in CI; including means manually rebuilding.

---

### F-13 — Step 1 Reads Wrong Files
- **Severity:** 🟡 Important
- **Area:** 4 — Path / Project Structure Accuracy
- **Finding:** Step 1 tells the agent to read `Web/Components/Layout/MainLayout.razor` etc. — all without the `src/` prefix. An agent reading these paths from the repo root will get "file not found" errors.
- **Recommendation:** Prefix all Step 1 paths with `src/`. Also add `src/Web/Features/` to the exploration list so the agent understands the actual page structure before rewriting.

---

### F-14 — Bootstrap Icons Still Referenced in NavMenu
- **Severity:** 🟡 Important
- **Area:** 2 — Completeness of Bootstrap Removal
- **Finding:** The actual `NavMenu.razor` uses Bootstrap Icons via inline SVG background images in `NavMenu.razor.css` (`.bi-house-door-fill-nav-menu`, `.bi-plus-square-fill-nav-menu`, etc.). The skill mentions removing `NavMenu.razor.css` but doesn't specifically address replacing these icons. After deletion, nav items will render without icons.
- **Recommendation:** In Step 5, explicitly state: "Replace Bootstrap Icon SVG backgrounds from `NavMenu.razor.css` with Heroicons (inline SVG) or equivalent. Do not simply delete without replacement."

---

### F-15 — Dark Mode Persistence Across Blazor Reconnects
- **Severity:** 🟡 Important
- **Area:** 7 — Missing Steps
- **Finding:** The JS theme initialiser runs once on page load. After a Blazor circuit reconnect (SignalR drop/reconnect), Blazor re-renders the DOM but does not re-run the initial JS. If the dark class is on `<html>` it should survive reconnect. However if the theme state is tracked in Blazor component state rather than on the DOM element, it could get out of sync.
- **Recommendation:** Document that theme state must be stored on `<html>` or `<body>` elements (outside the Blazor render tree) and not in Blazor `@code` blocks. The current JS approach using `document.documentElement.classList` is correct, but this should be explicitly noted in Step 7.

---

### F-16 — No @layer Usage for Custom CSS
- **Severity:** 🟢 Enhancement
- **Area:** 10 — CSS Custom Properties Strategy
- **Finding:** The skill adds custom CSS to `app.css` without `@layer` directives. Tailwind v3 uses `@layer base`, `@layer components`, `@layer utilities`. Custom rules outside a layer have higher specificity than Tailwind utilities and may conflict with `!important` utilities.
- **Recommendation:** Wrap all custom rules in appropriate `@layer` blocks. Theme CSS variables go in `@layer base`, form validation helpers in `@layer components`, custom utility classes in `@layer utilities`.

---

### F-17 — No .cshtml Files in Content Glob
- **Severity:** 🟢 Enhancement
- **Area:** 9 — Tailwind Configuration Quality
- **Finding:** The content glob only covers `.{razor,html}`. If any `.cshtml` Razor Pages exist (or error pages), their Tailwind classes won't be included.
- **Recommendation:** Add `cshtml` to the glob: `'./src/Web/**/*.{razor,html,cshtml,cs}'`

---

### F-18 — Verification Step Is Insufficient
- **Severity:** 🟢 Enhancement
- **Area:** 7 — Missing Steps
- **Finding:** Step 9 verification only checks visual rendering and toggle behavior. It doesn't verify: form validation states work, auth-gated nav shows/hides correctly, role-protected pages still enforce auth, and Blazor circuit reconnect restores theme.
- **Recommendation:** Expand the verification checklist:
  - Submit an invalid form — confirm red validation messages appear
  - Log in as Author — confirm "New Post" link appears
  - Log in as Admin — confirm "Manage Users" link appears
  - Disconnect and reconnect (DevTools → Network → Go offline → Go online) — confirm theme persists
  - Resize to mobile — confirm hamburger menu opens/closes

---

## Priority Action List

These 5 changes deliver the highest impact on skill completeness:

| Priority | Fix | Why |
|----------|-----|-----|
| **1** | **Fix all paths from `./Web/` to `./src/Web/`** (F-01, F-13) | Every step fails without this. Agent cannot read any file or run any command. |
| **2** | **Inline all reference file contents** (F-07) | Steps 3–6 are completely unexecutable without actual content to write. The skill is a stub. |
| **3** | **Add Tailwind safelist for dynamic theme classes** (F-04) | Themes will be purged in production builds. Users will see no color change on the deployed app. |
| **4** | **Add Blazor form validation CSS** (F-06) and **preserve AuthorizeView guards** (F-10) | Forms and access control are core functionality. Losing them breaks the app for the MyBlog use case. |
| **5** | **Resolve v3 vs v4 ambiguity and fix content glob** (F-08, F-03) | Mismatched version docs cause agents to attempt incompatible setup. Wrong glob means Tailwind purges valid classes from Features pages. |

---

## Summary Assessment

The skill is a **good starting blueprint** but is not executable as written. The reference file gap alone (F-07) means any agent running it will stall. The path errors (F-01) mean every command fails. The VSA-specific issues (F-02, F-10) mean the result would be wrong for this project even if it ran. The skill needs a significant revision pass before it can be reliably executed by the Squad.

# CI/CD Workflow for Pull Requests

**Date:** 2025-01-29  
**Author:** Boromir (DevOps)  
**Status:** Implemented  
**PR:** #5

## Context

MyBlog had NO build or test automation in GitHub Actions. The only existing workflows were squad automation (routing, standups). This created risk:
- No validation that PRs build successfully before merge
- No test execution feedback on code changes
- No code coverage visibility
- Manual local testing only

## Decision

Created `.github/workflows/ci.yml` to provide automated build checks and test execution for all pull requests.

## Implementation

### Workflow Configuration

**Triggers:**
- `pull_request` events targeting `main` or `squad/**` branches
- `push` events to `main` branch

**Platform:**
- Runs on: `ubuntu-latest`
- .NET version: 10.0.x preview (matches global.json SDK 10.0.100)

**Permissions:**
- `contents: read` — checkout code
- `checks: write` — publish test results
- `pull-requests: write` — post coverage comments

### Pipeline Steps

1. **Checkout** — actions/checkout@v4
2. **Setup .NET** — actions/setup-dotnet@v4 with dotnet-quality: preview
3. **Cache NuGet** — actions/cache@v4 keyed by csproj + Directory.Packages.props
4. **Restore** — dotnet restore MyBlog.slnx
5. **Build** — dotnet build --configuration Release --no-restore (with CI=true env)
6. **Test: Architecture** — 6 architecture tests with coverage
7. **Test: Unit** — 61 unit tests with coverage (92.04% baseline)
8. **Test: Integration** — 9 integration tests with Testcontainers
9. **Report Tests** — dorny/test-reporter@v1 for inline PR annotations
10. **Upload Coverage** — actions/upload-artifact@v4 (30-day retention)
11. **Coverage Summary** — irongut/CodeCoverageSummary@v1.3.0
12. **Coverage Comment** — marocchino/sticky-pull-request-comment@v2

### Key Technical Decisions

**Why CI=true for build?**
- Web.csproj has custom Tailwind CSS build target
- Target checks `Condition="'$(CI)' != 'true'"` to skip when CI=true
- Avoids npm/Node.js dependency in CI environment
- Tailwind output (wwwroot/css/tailwind.css) is committed to repo

**Why --no-build for tests?**
- Build step already compiles in Release mode
- Passing --no-build to dotnet test avoids redundant compilation
- Saves ~30-60 seconds per test run
- Ensures tests run against exact build output

**Why separate test result directories?**
- Architecture: `./test-results/architecture`
- Unit: `./test-results/unit`
- Integration: `./test-results/integration`
- Clean separation for artifact uploads
- Easier debugging if one suite fails

**Why sticky-pull-request-comment?**
- Updates same comment on subsequent pushes
- Avoids cluttering PR with multiple coverage comments
- Uses `recreate: true` to replace content entirely

**Testcontainers handling:**
- No special CI configuration needed
- GitHub Actions runners have Docker pre-installed
- Testcontainers.MongoDb pulls image automatically on first test run
- May add Docker layer caching if startup becomes bottleneck

### Coverage Configuration

Unit.Tests already configured with:
```xml
<CollectCoverage>true</CollectCoverage>
<CoverletOutputFormat>cobertura</CoverletOutputFormat>
<Threshold>89</Threshold>
<ThresholdType>line</ThresholdType>
```

All test projects reference `coverlet.collector` 6.0.4.

CI workflow uses `--collect:"XPlat Code Coverage"` which leverages coverlet.collector to generate coverage.cobertura.xml files.

## Alternatives Considered

**codecov.io vs irongut/CodeCoverageSummary:**
- Chose irongut — simpler, no external account
- Codecov offers historical trends but adds complexity
- Can migrate to Codecov later if needed

**ReportGenerator for HTML reports:**
- Not needed — markdown summary sufficient for PR feedback
- Can add later if detailed coverage browsing is needed

**Matrix builds (multiple .NET versions):**
- Not needed — project targets net10.0 only
- global.json pins SDK to 10.0.100
- Single version reduces CI minutes

**Parallel test execution:**
- Could run three test suites in parallel jobs
- Chose sequential for simplicity (total test time ~30s)
- Revisit if test suite grows significantly

## Consequences

**Positive:**
- Every PR now gets build validation
- Test failures appear inline on PR diff
- Code coverage visible on every PR
- NuGet caching speeds up builds (~2-3x faster)
- CI acts as documentation of "how to build this project"

**Negative:**
- CI minutes cost (though free for public repos)
- Testcontainers image pulls add ~30-60s to first run
- Failed builds block merges (this is actually a feature)

**Neutral:**
- Coverage threshold enforcement stays in Unit.Tests.csproj (local)
- CI reports coverage but doesn't fail on threshold (yet)
- Can add coverage gates later if team desires

## Follow-up Actions

1. Monitor first workflow run on PR #5
2. Adjust coverage reporting format if output is too verbose
3. Consider adding Docker layer caching if Testcontainers startup is slow
4. Document "how to run tests locally" in CONTRIBUTING.md
5. May add status badge to README.md once workflow stabilizes

## References

- PR #5: https://github.com/mpaulosky/MyBlog/pull/5
- dorny/test-reporter: https://github.com/dorny/test-reporter
- irongut/CodeCoverageSummary: https://github.com/irongut/CodeCoverageSummary
- Testcontainers: https://dotnet.testcontainers.org/

# Security Findings — PR #2 (squad/coverage-test-hardening-main)

**Raised by:** Gandalf (Security Officer)
**Date:** 2025-07
**Status:** Requires fix before merge

## Issue 1 — [HIGH] Open Redirect in `/Account/Login` endpoint

**Location:** `src/Web/Program.cs`, line 111

**Description:** The `returnUrl` query parameter is passed without validation directly to `WithRedirectUri(returnUrl ?? "/")`. An attacker can craft `/Account/Login?returnUrl=https://evil.com` to redirect users to external phishing sites after a successful Auth0 login.

**Recommended Fix:**
```csharp
// In Program.cs — validate returnUrl before using it
var safeReturnUrl = (!string.IsNullOrEmpty(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
    ? returnUrl
    : "/";
var props = new LoginAuthenticationPropertiesBuilder()
    .WithRedirectUri(safeReturnUrl)
    .Build();
```

---

## Issue 2 — [MEDIUM] Null dereference risk on `OnTokenValidated` handler

**Location:** `src/Web/Program.cs`, line 49

**Description:** `existingOnTokenValidated` may be null if Auth0 SDK does not pre-set the event. Calling `await existingOnTokenValidated(context)` will throw `NullReferenceException` and break all logins.

**Recommended Fix:**
```csharp
if (existingOnTokenValidated is not null)
{
    await existingOnTokenValidated(context);
}
```

---

## Decision Requested

Both issues must be resolved before PR #2 is merged. The open redirect in particular is a security requirement that must not be shipped.

# Decision: File Headers Required for All Test Files

**Date:** 2025-04-18  
**Proposed by:** Gimli (Tester)  
**Status:** Pending team review

## Context

During PR #2 review, discovered that new test files lack copyright headers, while production code consistently includes them. Gimli's charter (Critical Rule #6) requires "File header REQUIRED (block format with copyright)" but the format was not previously documented in team decisions.

## Decision

ALL test files (unit, integration, architecture, bUnit) MUST include the standard block-format copyright header:

```csharp
// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     {FileName}.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : MyBlog
// Project Name :  {ProjectName}
// =======================================================
```

**Project Name mapping:**
- `tests/Unit.Tests/**/*.cs` → `Unit.Tests`
- `tests/Architecture.Tests/**/*.cs` → `Architecture.Tests`
- `tests/Integration.Tests/**/*.cs` → `Integration.Tests`

## Rationale

1. Production code (`src/`) already uses this header format consistently (e.g., `src/Domain/Abstractions/Result.cs`)
2. Headers provide legal clarity (copyright), authorship, and project context
3. Consistency across test and production code maintains professional standards
4. Gimli's charter explicitly requires this per Critical Rule #6

## Impact

- PR #2 must add headers to 6 new test files before merge
- Future test files must include headers from creation
- No impact on test execution or coverage

## Alternatives Considered

1. **No headers for tests** — Rejected; violates charter, creates inconsistency with production code
2. **Simplified header** — Rejected; existing production code already uses block format

## Follow-Up Actions

- [ ] Scribe merges this decision into `.squad/decisions.md`
- [ ] Ralph validates PR #2 includes headers before allowing merge

# Decision: Move Tailwind CSS files to wwwroot/css/

**Agent:** Legolas (Frontend/Blazor Developer)  
**Date:** 2025-07-19  
**Branch:** feature/tailwind-migration  
**Status:** Implemented

---

## Decision

Tailwind CSS source and compiled output files are stored under `wwwroot/css/` rather than the `wwwroot/` root.

- **Source:** `src/Web/wwwroot/css/app.css`
- **Compiled output:** `src/Web/wwwroot/css/tailwind.css`

Additionally, the stale `wwwroot/lib/` directory (Bootstrap libman artifacts) has been removed entirely.

---

## Rationale

1. **Cleaner wwwroot structure:** `wwwroot/` root should contain only static entry-point assets (favicon, etc.). CSS belongs in a dedicated `css/` subdirectory, matching Blazor project conventions.
2. **Matches .NET Blazor template conventions:** Default Blazor project templates put styles in `wwwroot/css/`.
3. **lib/ was not empty:** Bootstrap 5 distribution files (60+ CSS/JS files) were still present from the original libman install. These are dead weight after migration and were safely deleted.

---

## Impact on Other Files

| File | Change |
|------|--------|
| `src/Web/wwwroot/css/app.css` | Moved from `wwwroot/app.css`; `@source` paths updated to `../../Components/**` and `../../Features/**` |
| `src/Web/wwwroot/css/tailwind.css` | Moved from `wwwroot/tailwind.css` |
| `src/Web/wwwroot/lib/` | Deleted (Bootstrap libman artifacts) |
| `src/Web/Components/App.razor` | `<link>` changed to `css/tailwind.css`; `@Assets["css/app.css"]` |
| `package.json` (repo root) | `tw:build` and `tw:watch` scripts updated to new `-i` and `-o` paths |
| `.gitignore` | Updated gitignore entry to `src/Web/wwwroot/css/tailwind.css` |
| `tailwind-migration/SKILL.md` | All path references updated to reflect new structure |

---

## @source Directive Note

When `app.css` lived at `wwwroot/app.css`, the @source paths were:
```css
@source "../Components/**/*.{razor,html,cshtml}";
@source "../Features/**/*.{razor,html,cshtml}";
```
(`../` resolved to `src/Web/` from `wwwroot/`)

After moving to `wwwroot/css/app.css`, one more `../` is needed:
```css
@source "../../Components/**/*.{razor,html,cshtml}";
@source "../../Features/**/*.{razor,html,cshtml}";
```
(`../../` resolves to `src/Web/` from `wwwroot/css/`)

---

## Verification

`dotnet build src/Web/Web.csproj` succeeded — Tailwind compiled in 64ms with 0 warnings, 0 errors.

# Decision: Simplified Theme Architecture

**Date:** 2025-01-29  
**Author:** Legolas (Frontend Developer)  
**Status:** Implemented

## Context

The MyBlog application previously used 8 distinct theme classes (`theme-blue-dark`, `theme-red-light`, etc.) with custom OKLCH color values. This approach created several issues:

- **Over-engineering**: 8 theme classes (4 colors × 2 brightness modes) led to combinatorial explosion
- **Non-idiomatic Tailwind**: Used custom OKLCH values instead of standard Tailwind color palettes
- **Coupled concerns**: Color and brightness were tightly coupled in a single theme string
- **Maintenance burden**: Changes required updating multiple theme classes and semantic variables
- **Storage complexity**: Unified `tailwind-color-theme` key stored combined state (e.g., `theme-blue-dark`)

## Decision

We simplified the theme system to use orthogonal concerns:

### Architecture Changes

1. **4 Color Swap Classes** (instead of 8 theme classes):
   - `:root.color-blue`, `:root.color-red`, `:root.color-green`, `:root.color-yellow`
   - Each swaps CSS custom properties (`--primary-50` through `--primary-950`)
   - Uses standard Tailwind hex color values (not custom OKLCH)

2. **Native Dark Mode** (instead of theme-specific brightness):
   - Uses `.dark` class on `<html>` for brightness control
   - Components use Tailwind's native `dark:` variant (e.g., `bg-primary-400 dark:bg-primary-800`)
   - Brightness is decoupled from color selection

3. **Split Storage Keys** (instead of unified key):
   - `theme-color`: Stores color preference ('blue', 'red', 'green', 'yellow')
   - `theme-mode`: Stores brightness preference ('light', 'dark')
   - Each concern is independently queryable

4. **Standard Tailwind Palettes**:
   - Replaced custom OKLCH values with Tailwind's default hex colors
   - More maintainable and familiar to Tailwind users
   - Automatically generates utilities like `bg-primary-600`, `dark:bg-primary-900`

5. **Common Element Standardization**:
   - Added base styling for `body`, `a`, `h1`-`h3` with light/dark variants
   - Created reusable `.nav-link`, `.btn-primary`, `.card` component classes
   - Removed semantic variables (`--color-canvas`, `--color-surface`, etc.) in favor of explicit Tailwind classes

### Migration Strategy

- Anti-FOUC IIFE automatically migrates old `tailwind-color-theme` format to new split keys
- Existing users' preferences are preserved on first load after update
- Storage format: `theme-blue-dark` → `theme-color: 'blue'` + `theme-mode: 'dark'`

## Consequences

### Positive

- **Simpler mental model**: Color and brightness are clearly separate concerns
- **Less code**: 4 color classes instead of 8 theme classes
- **More idiomatic**: Uses standard Tailwind patterns (colors, `dark:` variant)
- **Easier maintenance**: Changes to one concern don't affect the other
- **Better DX**: Developers can use familiar Tailwind utilities

### Negative

- **Breaking change**: Components using removed semantic vars (`bg-canvas`, `text-content`, etc.) needed updates
- **Migration complexity**: Anti-FOUC IIFE now handles format conversion (though transparent to users)

### Neutral

- **Same FOUC prevention**: Still uses IIFE + MutationObserver architecture
- **Same UI**: Theme toggle UI remains functionally identical

## Implementation Details

### Files Changed

- `app.css`: Replaced 8 theme classes with 4 color swap classes; added common element styling
- `theme.js`: Rewrote to use split storage keys and orthogonal color/brightness methods
- `App.razor`: Updated IIFE to use split keys with migration logic; changed MutationObserver to detect `color-{name}` classes
- All `.razor` components: Updated from semantic vars to Tailwind utility classes

### Technical Notes

- Uses Tailwind v4 syntax (`@theme inline`, `@custom-variant`)
- CSS variables use simple `--primary-{shade}` naming (not `--color-primary-{shade}`)
- Components use explicit Tailwind classes (e.g., `bg-white dark:bg-gray-800`) instead of semantic vars
- Primary color utilities (`bg-primary-*`, `text-primary-*`) are generated at build time from CSS variables

## Related Decisions

- This supersedes the original 8-theme system documented in `.squad/skills/blazor-tailwind-theme-persistence/SKILL.md`
- The skill file should be updated to mark old patterns as deprecated

## References

- [Tailwind CSS v4 Documentation](https://tailwindcss.com/docs/v4-beta)
- [CSS Custom Properties (MDN)](https://developer.mozilla.org/en-US/docs/Web/CSS/Using_CSS_custom_properties)
- Original skill file: `.squad/skills/blazor-tailwind-theme-persistence/SKILL.md`

# Tailwind CSS v4.2 Migration Complete

**Date:** 2025-04-17  
**Agent:** Legolas  
**Status:** ✅ Complete - Build Successful

## Summary

Successfully migrated MyBlog Blazor Web project from Bootstrap 5 to Tailwind CSS v4.2 using CSS-first configuration. All 11 Razor component files converted to semantic token classes with 4-color theme system (red, blue, green, yellow) and light/dark mode support. Navigation changed from left sidebar to horizontal top bar. Build verified with zero errors.

## Files Modified

### Package & Configuration
- `package.json` - Created with Tailwind v4.2 and @tailwindcss/cli
- `.gitignore` - Added node_modules/ and tailwind.css exclusions
- `src/Web/Web.csproj` - Added BuildTailwind MSBuild target (runs before Build)
- `src/Web/wwwroot/app.css` - Completely replaced with Tailwind v4 CSS-first config (69 lines)

### Core Layout Components
- `src/Web/Components/App.razor` - Removed Bootstrap CSS, added Tailwind CSS + theme init script
- `src/Web/Components/Layout/MainLayout.razor` - Rewritten with vertical layout + semantic tokens
- `src/Web/Components/Layout/NavMenu.razor` - Rewritten as horizontal top bar with themes + dark mode
- `src/Web/Components/Layout/ReconnectModal.razor.css` - Updated colors to semantic tokens

### Feature Pages (BlogPosts)
- `src/Web/Features/BlogPosts/List/Index.razor` - Table, alerts, buttons migrated
- `src/Web/Features/BlogPosts/Create/Create.razor` - Form controls, labels, buttons migrated
- `src/Web/Features/BlogPosts/Edit/Edit.razor` - Form controls + concurrency alert migrated
- `src/Web/Features/BlogPosts/Delete/ConfirmDeleteDialog.razor` - Modal migrated to fixed overlay pattern

### Feature Pages (UserManagement)
- `src/Web/Features/UserManagement/ManageRoles.razor` - Table + outline buttons migrated

### Template Pages
- `src/Web/Components/Pages/Counter.razor` - Button migrated
- `src/Web/Components/Pages/Weather.razor` - Table migrated

## Files Deleted

- `src/Web/Components/Layout/MainLayout.razor.css`
- `src/Web/Components/Layout/NavMenu.razor.css`
- `src/Web/wwwroot/lib/bootstrap/` (entire directory)

## Key Implementation Details

### Tailwind v4 CSS-First Configuration
```css
@import "tailwindcss";
@source "../Components/**/*.razor";
@source "../Features/**/*.razor";
@custom-variant dark (&:where(.dark, .dark *));
@theme inline {
  --color-canvas: #f9fafb;
  --color-surface: #ffffff;
  /* ... semantic tokens ... */
}
```

### Semantic Token System
- **Surfaces:** `bg-canvas`, `bg-surface`, `bg-primary`, `hover:bg-primary-hover`
- **Text:** `text-content`, `text-muted`, `text-primary`
- **Borders:** `border-edge`, `border-primary`
- All backed by CSS custom properties with separate light/dark mode values
- Theme colors applied via `body.theme-{red|blue|green|yellow}` classes

### Theme State Management
- JavaScript in `App.razor` runs before Blazor hydration (prevents flash)
- State stored on `<html>` (dark mode) and `<body>` (color theme)
- Persisted in localStorage: "darkMode" (boolean), "colorTheme" (class name)
- Blazor calls via IJSRuntime: `JS.InvokeVoidAsync("setTheme", "theme-red")`
- No inline `style=""` attributes used anywhere

### Navigation Component
- Horizontal top bar with brand, nav links, theme switcher, dark mode toggle
- Pure CSS hamburger using peer checkbox pattern (no Blazor state)
- AuthorizeView blocks preserved with exact roles: "Author,Admin" and "Admin"
- Inline Heroicon SVG (bars-3) replacing Bootstrap Icons

### Modal Pattern
- Fixed overlay: `fixed inset-0 z-[100] flex items-center justify-center bg-black/50`
- Centered dialog with `@onclick:stopPropagation="true"` to prevent closing on content click
- Semantic tokens: `bg-surface`, `border-edge`, `text-content`

### Build Integration
- MSBuild target runs `npm run tw:build` before Build (skipped when CI=true)
- Fixed WorkingDirectory: `$(MSBuildProjectDirectory)/../..` (navigate to repo root)
- For hot reload: must run `npm run tw:watch` in parallel terminal
- Output: 28KB minified CSS at `src/Web/wwwroot/tailwind.css` (excluded from git)

## Bootstrap → Tailwind Mappings

| Bootstrap | Tailwind Equivalent |
|-----------|---------------------|
| `btn btn-primary` | `px-4 py-2 rounded font-medium text-white bg-primary hover:bg-primary-hover transition` |
| `btn btn-secondary` | `px-4 py-2 rounded font-medium border border-edge text-content hover:bg-surface transition` |
| `btn btn-danger` | `px-4 py-2 rounded font-medium bg-red-600 text-white hover:bg-red-700 transition` |
| `btn btn-sm` | Add `text-sm px-3 py-1` to base button classes |
| `btn-outline-success` | `border border-green-600 text-green-600 hover:bg-green-50 dark:hover:bg-green-900/20` |
| `form-control` | `w-full rounded border border-edge bg-surface text-content px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary` |
| `form-label` | `block text-sm font-medium text-content mb-1` |
| `alert alert-danger` | `rounded-lg border border-red-300 bg-red-50 text-red-700 px-4 py-3 text-sm` + `role="alert"` |
| `alert alert-warning` | `rounded-lg border border-yellow-300 bg-yellow-50 text-yellow-700 px-4 py-3 text-sm` |
| `table` | `w-full text-sm text-left text-content` wrapped in `rounded-lg shadow bg-surface overflow-hidden` |
| `table-striped` | `odd:bg-canvas even:bg-surface` on `<tr>` |
| `modal` + `modal-backdrop` | `fixed inset-0 z-[100] flex items-center justify-center bg-black/50` |

## Verification Results

### Build Status
```bash
$ dotnet build src/Web/Web.csproj
Build succeeded in 2.8s
  Web net10.0 succeeded (1.2s) → src/Web/bin/Debug/net10.0/Web.dll
```

### CSS Compilation
```bash
$ npm run tw:build
Compiled in 103ms
Output: src/Web/wwwroot/tailwind.css (28KB)
```

### Bootstrap Class Audit
Final `grep` confirmed zero Bootstrap classes remaining in Razor files. All `alert`, `btn`, `form-control`, `table`, `modal` instances converted to Tailwind.

## Challenges & Solutions

1. **MSBuild Working Directory**
   - Problem: `$(SolutionDir)` was undefined, build failed with "working directory does not exist"
   - Solution: Changed to `$(MSBuildProjectDirectory)/../..` for relative path from `src/Web/` to repo root

2. **Modal Click-Through**
   - Problem: Clicking modal dialog content closed the modal
   - Solution: Added `@onclick:stopPropagation="true"` to dialog div

3. **Outline Button Dark Mode**
   - Problem: Outline buttons (green/red) invisible in dark mode
   - Solution: Added explicit dark mode hover states: `dark:hover:bg-green-900/20`

## Developer Workflow

### Development Mode (Hot Reload)
```bash
# Terminal 1: Blazor with hot reload
dotnet watch run --project src/Web

# Terminal 2: Tailwind watch mode
npm run tw:watch
```

### Production Build
```bash
# MSBuild target automatically runs Tailwind build
dotnet build src/Web/Web.csproj
```

## Constraints Met

✅ Never used inline `style=""` attributes - all styling via semantic token classes  
✅ Preserved all AuthorizeView roles exactly ("Author,Admin" and "Admin")  
✅ Used only semantic token classes (bg-surface, text-content, etc.) not arbitrary values  
✅ Tailwind v4's CSS-first configuration (no tailwind.config.js)  
✅ Pure CSS mobile hamburger (peer checkbox pattern)  
✅ Theme state management without inline styles  
✅ Form validation classes styled in @layer components  
✅ NavLink .active class styled  
✅ ARIA attributes added where Bootstrap provided them  

## Recommendation

**Status:** Ready for review and testing

**Next Steps:**
1. Manual browser testing: verify theme switching, dark mode, mobile nav, form validation
2. Test Blazor circuit reconnects to verify theme persistence
3. Test all AuthorizeView scenarios with different user roles
4. Performance audit: verify CSS file size acceptable (~28KB minified)
5. Consider adding Playwright/browser tests for theme persistence

**No Issues Found:**
- Build successful with zero warnings
- All files migrated without compromises
- No Bootstrap dependencies remaining
- Semantic token system working as designed

---

**Agent Notes:** Migration completed using Tailwind CSS v4.2 CSS-first approach with semantic tokens. MSBuild integration required WorkingDirectory fix but otherwise worked perfectly. All constraints met, zero inline styles used.

# Legolas — Tailwind Migration Skill Review
**Date:** 2025-07-19  
**Reviewer:** Legolas (Frontend/Blazor specialist)  
**Skill reviewed:** `tailwind-migration` at `/home/mpaulosky/.config/squad/.github/skills/tailwind-migration/SKILL.md`  
**Requested by:** Michael Paulosky

---

## Summary

The skill provides a useful structural migration guide, but has **three critical gaps** that would cause immediate breakage in this project, plus several important issues that would degrade the user experience and require rework. The `./references/` directory referenced throughout the skill does not exist — the skill is self-referential but incomplete.

---

## Findings

---

### 1. Tailwind v3 vs v4 Version Conflict
**Severity:** 🔴 Critical  
**Component:** Build setup (`package.json`, `app.css`, `tailwind.config.js`)  

**Issue:**  
The skill description header says *"Tailwind CSS v4+"* but the `package.json` specifies `"tailwindcss": "^3.4.0"` and uses v3 directives:
```css
@tailwind base;
@tailwind components;
@tailwind utilities;
```
v4 uses `@import "tailwindcss"` and has **no** `tailwind.config.js` — all config is done in CSS via `@theme {}`. These are mutually incompatible. An agent following the skill literally will attempt v3 setup with a v4 mental model (or vice versa), producing a broken build. v4 is the current release as of 2025; v3 is maintenance-only.

**Fix:**  
Pick one version and be explicit. For v3 (safer, more ecosystem support in 2025):  
- Remove "v4+" from the description  
- Keep `tailwind.config.js` and `@tailwind` directives as documented  
- State clearly: *"This skill targets Tailwind CSS v3.4.x"*

For v4: rewrite Steps 2c and 2d entirely, removing `tailwind.config.js` and using `@import "tailwindcss"` with `@theme {}` blocks for dark mode and content scanning.

---

### 2. Tailwind Content Path Mismatch — Feature Pages Not Scanned
**Severity:** 🔴 Critical  
**Component:** `tailwind.config.js` content array  

**Issue:**  
The `tailwind.config.js` content array is:
```js
content: [
  './Web/Components/**/*.{razor,html}',
  './Web/wwwroot/index.html',
],
```

Two problems:
1. The MyBlog solution structure is `src/Web/`, not `Web/` — paths are relative to where `package.json` sits (solution root). The correct prefix would be `./src/Web/`.
2. The project uses VSA — feature pages live in `src/Web/Features/BlogPosts/` and `src/Web/Features/UserManagement/`, which the glob `./Web/Components/**` does NOT match.

This means ALL Tailwind classes used in `Create.razor`, `Edit.razor`, `List/Index.razor`, `Delete/ConfirmDeleteDialog.razor`, and `ManageRoles.razor` will be purged from the production CSS. Forms, tables, buttons — everything on feature pages will be unstyled.

**Fix:**  
```js
content: [
  './src/Web/Components/**/*.{razor,html}',
  './src/Web/Features/**/*.{razor,html}',
  './src/Web/wwwroot/**/*.{html,js}',
],
```
Additionally, add a `safelist` for any theme classes constructed via C# string interpolation (e.g., `$"theme-{color}"`), since Tailwind cannot detect dynamic class names at build time:
```js
safelist: [
  'theme-red', 'theme-blue', 'theme-green', 'theme-yellow',
],
```

---

### 3. All Reference Files Are Missing
**Severity:** 🔴 Critical  
**Component:** Skill completeness / agent execution  

**Issue:**  
Steps 3–6 all instruct the agent to copy content from `./references/` files:
- `./references/app.css`
- `./references/MainLayout.razor`
- `./references/NavMenu.razor`
- `./references/pages/` (directory)

The skill directory contains **only** `SKILL.md`. There are no reference files. An agent executing this skill will fail at Step 3 and everything downstream. The skill is structurally incomplete.

**Fix:**  
Either:
- (A) Create the reference files in the skill directory so agents can read and apply them, OR  
- (B) Inline all the CSS/Razor content directly in the SKILL.md (longer, but self-contained)

Option B is more robust. The Theme Color Reference table in the skill is a good model — expand that approach to include the actual CSS custom properties block inline in Step 3, and inline the full Razor templates in Steps 4 and 5.

---

### 4. Blazor Form Validation Styles Lost on app.css Replacement
**Severity:** 🟡 Important  
**Component:** `app.css` → Blazor validation visual feedback  

**Issue:**  
The current `app.css` contains these Blazor-generated class styles:
```css
.valid.modified:not([type=checkbox]) { outline: 1px solid #26b050; }
.invalid { outline: 1px solid #e50000; }
.validation-message { color: #e50000; }
```
Bootstrap styles these automatically via its form classes. Tailwind does NOT. Step 3 replaces `app.css` entirely, and since the reference file doesn't exist, there's no guarantee these rules are preserved. All form validation visual feedback (`EditForm`, `DataAnnotationsValidator`, `ValidationSummary`) will be invisible after migration.

The current `Create.razor` and `Edit.razor` use `EditForm` with `DataAnnotationsValidator` and `ValidationSummary` — this will silently break.

**Fix:**  
Add an explicit `@layer components` block to the new `app.css` for Blazor's emitted validation classes:
```css
@layer components {
  .valid.modified:not([type=checkbox]) {
    @apply outline outline-1 outline-green-500;
  }
  .invalid {
    @apply outline outline-1 outline-red-500;
  }
  .validation-message {
    @apply text-red-600 text-sm mt-1;
  }
}
```
Document this as a required section in Step 3.

---

### 5. NavMenu AuthorizeView Structure Not Preserved
**Severity:** 🟡 Important  
**Component:** `NavMenu.razor` rewrite  

**Issue:**  
The current `NavMenu.razor` contains carefully structured `<AuthorizeView>` wrappers:
- `Roles="Author,Admin"` for "New Post" link
- `Roles="Admin"` for "Manage Users" link  
- `<Authorized>` / `<NotAuthorized>` for Login/Logout toggle

The skill says to use the template at `./references/NavMenu.razor` (which doesn't exist), and its description gives no indication that these auth structures must be preserved. An agent following the skill naively will write a nav bar without auth-awareness, making all protected links visible to unauthenticated users (client-side only — server auth still protects the routes, but the nav is confusing and unprofessional).

**Fix:**  
The skill's NavMenu template must include the full `<AuthorizeView>` structure. Document explicitly:

> "This project uses Auth0. The new top navigation **must** preserve all `<AuthorizeView Roles="...">` wrappers from the existing NavMenu.razor. Do not omit or collapse these."

The new top-bar template should mirror the existing auth structure adapted to horizontal layout:
```razor
<AuthorizeView Roles="Author,Admin">
    <Authorized>
        <NavLink class="..." href="blog/create">New Post</NavLink>
    </Authorized>
</AuthorizeView>
<AuthorizeView Roles="Admin">
    <Authorized>
        <NavLink class="..." href="admin/users">Manage Users</NavLink>
    </Authorized>
</AuthorizeView>
```

---

### 6. Hamburger Menu State Management — Blazor vs Pure CSS
**Severity:** 🟡 Important  
**Component:** `NavMenu.razor` mobile responsiveness  

**Issue:**  
The skill describes a "fixed top-0 left-0 right-0 z-50" top bar with a hamburger (`md:hidden`). The current NavMenu uses a CSS-driven checkbox toggle (`<input type="checkbox" class="navbar-toggler">`). The skill gives no implementation guidance on how the hamburger open/close state should be managed.

In Blazor Server (SignalR circuit), two approaches work:
1. **Pure CSS (peer utility):** `<input type="checkbox" id="menu-toggle" class="peer hidden">` + `<label for="menu-toggle">☰</label>` + `<div class="hidden peer-checked:flex ...">` — no JS or C# state needed, works even before Blazor hydrates
2. **C# state + `@onclick`:** `bool _menuOpen` field with `@onclick="() => _menuOpen = !_menuOpen"` — cleaner Blazor idiom but requires circuit

The skill should specify which approach and provide the implementation. Approach 1 is recommended for layout components since they render before full interactivity.

**Fix:**  
Document the CSS peer approach inline in Step 5, since it's more resilient and doesn't require JS interop:
```razor
<input type="checkbox" id="menu-toggle" class="peer hidden" />
<label for="menu-toggle" class="md:hidden cursor-pointer p-2" aria-label="Toggle navigation">
    ☰
</label>
<div class="hidden peer-checked:flex flex-col md:flex ...">
    <!-- nav links -->
</div>
```

---

### 7. NavLink `.active` Class Not Styled in Tailwind
**Severity:** 🟡 Important  
**Component:** `NavMenu.razor` — active link styling  

**Issue:**  
Blazor's `<NavLink>` component automatically adds the `active` CSS class to the anchor when the current URL matches. Bootstrap's nav styles `.nav-link.active` automatically. Tailwind has no default for `.active` — after migration, the active page indicator in the nav will be completely invisible.

**Fix:**  
Add to `app.css` `@layer components`:
```css
@layer components {
  .nav-link.active {
    @apply font-semibold border-b-2 border-current;
  }
}
```
Or use Tailwind's arbitrary variant in the NavLink's class attribute — though this requires knowing the active state in Blazor, which is not straightforward. The `@layer components` approach is simpler and cleaner.

---

### 8. JS Interop Pattern for Theme Switching Not Shown
**Severity:** 🟡 Important  
**Component:** Theme switcher in `NavMenu.razor` / `IJSRuntime`  

**Issue:**  
Step 7 defines `window.setTheme(t)` and `window.toggleDark()`. The skill does not show how to invoke these from Blazor components. There are two valid patterns, and the distinction matters:

- **HTML `onclick` attribute (static):** `<button onclick="window.toggleDark()">` — works without `IJSRuntime`, fires synchronously
- **Blazor `@onclick` handler:** `@onclick="async () => await JS.InvokeVoidAsync(\"toggleDark\")"` — requires `@inject IJSRuntime JS`, is async, preferred for Blazor Server

The layout components (`MainLayout`, `NavMenu`) render as interactive server components in this project. Using `@onclick` with `IJSRuntime` is more correct. Without guidance, an agent may mix the patterns or use `onclick=""` which doesn't integrate with Blazor's event system.

**Fix:**  
In Step 7, add a code sample showing the correct Blazor Server injection and call pattern:
```razor
@inject IJSRuntime JS

<button @onclick="ToggleDark">🌙</button>
<button @onclick='() => JS.InvokeVoidAsync("setTheme", "theme-red")'>Red</button>

@code {
    private async Task ToggleDark() =>
        await JS.InvokeVoidAsync("toggleDark");
}
```
Note: The inline script must be placed before `<script src="_framework/blazor.server.js">` in `App.razor` to ensure theme is applied before Blazor hydrates (avoids flash of unstyled theme).

---

### 9. Feature Pages Not in Step 6 Update Table
**Severity:** 🟡 Important  
**Component:** Step 6 — Page migration scope  

**Issue:**  
Step 6's table lists only the default Blazor template pages. The MyBlog project has feature pages with substantial Bootstrap usage that must also be migrated:

| Missing Feature Page | Bootstrap Classes Used |
|---|---|
| `Features/BlogPosts/List/Index.razor` | tables, buttons, badges |
| `Features/BlogPosts/Create/Create.razor` | `form-control`, `mb-3`, `form-label`, `btn`, `alert alert-danger` |
| `Features/BlogPosts/Edit/Edit.razor` | same as Create |
| `Features/BlogPosts/Delete/ConfirmDeleteDialog.razor` | buttons, modal-like card |
| `Features/UserManagement/ManageRoles.razor` | `table table-striped`, `btn btn-sm btn-outline-success/danger`, `alert alert-danger` |

**Fix:**  
Extend the Step 6 table:
```
| Features/BlogPosts/List/Index.razor       | Blog post list with Tailwind table styling       |
| Features/BlogPosts/Create/Create.razor    | Form fields using @layer components form styles  |
| Features/BlogPosts/Edit/Edit.razor        | Same as Create                                   |
| Features/BlogPosts/Delete/ConfirmDeleteDialog.razor | Danger confirmation card               |
| Features/UserManagement/ManageRoles.razor | Admin table with role assignment buttons         |
```

---

### 10. MSBuild Target Won't Re-Run During `dotnet watch`
**Severity:** 🟢 Enhancement  
**Component:** Step 2g — Build integration  

**Issue:**  
The MSBuild target `BeforeTargets="Build"` only fires during `dotnet build` / `dotnet run` (initial build). Hot reload (`dotnet watch run`) does not re-trigger `Build` targets — CSS class changes in razor files won't be picked up in the compiled `tailwind.css` during development.

**Fix:**  
The skill already mentions running `npm run tw:watch` in a separate terminal. Make this more prominent and note it's mandatory for development:

> "⚠️ **Hot reload note:** The MSBuild target only runs on full builds. During development, you **must** run `npm run tw:watch` in a parallel terminal alongside `dotnet watch run`. Without this, new Tailwind classes added during development won't appear until the next full build."

For Aspire-based projects, consider adding a `npm tw:watch` as an Aspire resource:
```csharp
builder.AddNpmApp("tailwind-watch", "../", "tw:watch");
```

---

### 11. ARIA Attributes on Replaced Bootstrap Components
**Severity:** 🟢 Enhancement  
**Component:** Accessibility — all components  

**Issue:**  
Bootstrap's nav, alert, and modal components include ARIA attributes by default (e.g., `role="navigation"`, `aria-label`, `aria-expanded`, `aria-controls`). The current NavMenu uses `aria-hidden="true"` on icons. Tailwind provides no ARIA scaffolding — these must be added manually to every replaced component.

**Fix:**  
Add an accessibility checklist to Step 5 (NavMenu) and Step 9 (Verify):
- Hamburger button: `aria-label="Toggle navigation"`, `aria-expanded="{_menuOpen}"`, `aria-controls="mobile-menu"`
- Mobile menu div: `id="mobile-menu"`
- Alert divs: `role="alert"`
- Nav landmark: `<nav aria-label="Main navigation">`

---

## What Would Break Without the Top 3 Critical Fixes

### 🔴 #1 — v3/v4 Version Conflict
**What breaks:** The build fails entirely. `@tailwind` directives are invalid syntax in v4; `tailwind.config.js` is ignored in v4. The agent will attempt one but get the other. No CSS output is generated. The app renders completely unstyled.

### 🔴 #2 — Content Path Mismatch
**What breaks:** Even if the build succeeds, all Tailwind classes used in Feature pages (`Create.razor`, `Edit.razor`, `ManageRoles.razor`, etc.) are purged from the production stylesheet. Forms, tables, buttons on all blog post and user management pages render with zero styling. The app appears broken to end users on every functional page.

### 🔴 #3 — Missing Reference Files
**What breaks:** The agent cannot complete Steps 3, 4, 5, or 6 because the files it's told to copy from don't exist. The migration stops at Step 2. `app.css` keeps Bootstrap directives, `MainLayout.razor` stays as the sidebar layout, `NavMenu.razor` is unchanged. The migration produces no visible result.

---

*Reviewed by Legolas — Frontend/Blazor Specialist, MyBlog Squad*  
*For questions, coordinate with Aragorn (architect) on the v3/v4 version decision before execution.*

# Decision: Auth0 Configuration via User Secrets

**Date:** 2026-04-18  
**Author:** Sam (Backend Developer)  
**Status:** Proposed

## Context

The Web app was crashing at startup with `ArgumentException: The value cannot be an empty string (Parameter 'ClientId')` because `appsettings.json` stores empty placeholder strings for Auth0 settings and no user secrets were configured.

## Decision

Auth0 credentials (`Auth0:Domain`, `Auth0:ClientId`, `Auth0:ClientSecret`) are **never stored in source-controlled config files**. They must be set via dotnet user-secrets on the Web project:

```bash
dotnet user-secrets set "Auth0:Domain" "<your-tenant>.auth0.com" --project src/Web
dotnet user-secrets set "Auth0:ClientId" "<your-client-id>" --project src/Web
dotnet user-secrets set "Auth0:ClientSecret" "<your-client-secret>" --project src/Web
```

`appsettings.Development.json` documents the required keys (with empty values) as a developer reference. `Program.cs` validates these at startup and throws a clear `InvalidOperationException` with setup instructions if they are missing.

## Rationale

- Security: Auth0 secrets must not be committed to source control
- Developer experience: A clear error message with instructions is far better than a cryptic middleware exception
- AppHost does not inject Auth0 credentials — this is intentional (Aspire manages infrastructure secrets like MongoDB/Redis, not application-level OAuth credentials)

## Consequences

- New developers cloning the repo must run the user-secrets commands before the app will start
- The error message in Program.cs serves as self-documenting setup instructions

### 2026-04-17 16:34:33 UTC: Optimistic Concurrency — Handler Layer
**By:** Sam
**What:** Added DbUpdateConcurrencyException handling to Edit and Delete MediatR handlers. Returns Result.Fail with ResultErrorCode.Concurrency. UI shows "post was modified" message when concurrency error occurs.
**Why:** Training project demonstrating optimistic concurrency pattern end-to-end.

## Implementation Details
- EditBlogPostHandler: Wrapped SaveChangesAsync in try/catch for DbUpdateConcurrencyException
- DeleteBlogPostHandler: Wrapped SaveChangesAsync in try/catch for DbUpdateConcurrencyException
- Edit.razor: Added concurrency error UI with warning alert showing "This post was modified by another user. Please reload the page to get the latest version."
- Index.razor: Added concurrency error UI to delete operation flow
- Used fully qualified namespace `global::Domain.Abstractions.ResultErrorCode.Concurrency` in Razor files to avoid namespace conflicts with RootNamespace setting

## Files Modified
- `/src/Web/Features/BlogPosts/Edit/EditBlogPostHandler.cs`
- `/src/Web/Features/BlogPosts/Delete/DeleteBlogPostHandler.cs`
- `/src/Web/Features/BlogPosts/Edit/Edit.razor`
- `/src/Web/Features/BlogPosts/List/Index.razor`

## Validation
- Build succeeded with 0 errors, 0 warnings
- dotnet build src/Web/Web.csproj completed successfully

---

# Decision: dev/main Branch Strategy with GitVersion

**Date:** 2026-04-18  
**Author:** Aragorn (Lead Developer), Boromir (DevOps), Copilot  
**Status:** ✅ Implemented (Phase 1-2 complete)

## Context

The team needed a branching model to support coordinated squad work, release control, and hotfix safety. The decision establishes a clear separation between development (`dev`) and production (`main`) branches with supporting governance.

## Decision

### 1. Branch Strategy

- **`dev`** — Primary integration branch where all squad work targets
  - Default branch for `clone` and new PRs
  - All `squad/*` branches open PRs to `dev`
  - CI required; branch protection moderate
  - Release candidate state after testing

- **`main`** — Release-only branch; receives code from `dev` via explicit release PRs
  - No direct commits (all via PR)
  - Strict branch protection (1 approval, all CI checks required)
  - Only accepts PRs from `dev` or `hotfix/*` branches
  - Triggers production deployment workflows
  - Tagged with semantic version on each release

- **`hotfix/*`** — Critical bug fixes that bypass `dev`
  - Branch from `main`, target `main` directly
  - Require 1 approval (+ Gandalf security review for security-critical hotfixes)
  - Must be immediately backported to `dev` after merge (via cherry-pick or backport branch)
  - Tagged with patch version increment

### 2. CI/CD Triggers

Updated `.github/workflows/ci.yml` triggers:
```yaml
on:
  pull_request:
    branches: [dev, main]
  push:
    branches: [dev, main]
```

This ensures:
- All PRs to `dev` or `main` undergo full CI validation
- Post-merge pushes to `dev` are validated
- Post-merge pushes to `main` can trigger deployment workflows

### 3. Release Process

Release workflow (`dev` → `main`):
1. Create release PR from `dev` to `main` with title `[RELEASE] vX.Y.Z - Description`
2. Checklist in PR: CI passed, changelog updated, version bumped, breaking changes documented
3. Require 1 approval
4. Squash merge to `main` (keeps main history clean)
5. Tag release: `git tag -a vX.Y.Z -m "Release vX.Y.Z: Description"`
6. Fast-forward `dev` to `main`: `git checkout dev && git merge --ff-only main && git push origin dev`

### 4. Versioning: GitVersion (SemVer)

GitVersion calculates semantic versions automatically:
- **`main` branch** → `MAJOR.MINOR.PATCH` (e.g., `1.3.0`)
- **`dev` branch** → Pre-release version (e.g., `1.4.0-alpha.1`)
- **`squad/*` branches** → Label-stamped pre-release (e.g., `1.4.0-pr.42`)

CI runs GitVersion before build to stamp assembly versions:
```yaml
- name: GitVersion
  uses: gittools/actions/gitversion/execute@v1
  with:
    versionSpec: '6.x'
    updateAssemblyInfo: false

- name: Stamp versions
  run: |
    dotnet build MyBlog.slnx -c Release \
      /p:Version=${{ steps.gitversion.outputs.nuGetVersion }} \
      /p:AssemblyVersion=${{ steps.gitversion.outputs.assemblySemVer }} \
      /p:FileVersion=${{ steps.gitversion.outputs.assemblySemFileVer }}
```

**Key decision:** GitVersion.yml already exists at repo root with full branch config. Leverage it without additional maintenance overhead.

### 5. Hotfix Backport Process

**Automated reminder** via `.github/workflows/hotfix-backport-reminder.yml`:
- Triggers on hotfix merges to `main`
- Auto-comments with cherry-pick instructions
- Prevents dev/main drift

**Manual process** if reminder fails:
```bash
git checkout dev
git pull origin dev
git merge main          # Fast-forward (preferred)
# OR
git cherry-pick {hotfix-commit-sha}  # If dev has diverged
git push origin dev
```

**Critical rule:** Never allow hotfix to exist only on `main`. Always backport immediately.

### 6. Branch Protection Rules

**`main` branch (strictest):**
- Require pull request reviews: 1 minimum
- Dismiss stale reviews on new commits: enabled
- Require status checks: enabled (CI workflow)
- Require branches up-to-date: enabled
- Require conversation resolution: enabled
- Restrict who can push: (administrators must follow PR process)
- Allow force pushes: disabled
- Allow deletions: disabled

**`dev` branch (moderate):**
- Require status checks: enabled (CI workflow)
- Require branches up-to-date: enabled
- Require conversation resolution: disabled (allow WIP work)
- Allow force pushes: disabled
- Allow deletions: disabled
- (Optional) 1 approval for `squad/*` branches to `dev`

### 7. PR Workflows

**Regular squad/feature branch PR:**
```
squad/my-feature → PR → dev (target dev as default base)
```

**Release PR:**
```
dev → PR → main (only PR type allowed to main; title: [RELEASE] vX.Y.Z - ...)
```

**Hotfix PR:**
```
hotfix/critical-bug → PR → main (only other PR type to main; requires 1 approval)
```

### 8. AI Agent Compatibility

**`.squad/routing.md` and `squad-issue-assign.yml` updated:**
- Default base branch hardcoded to `dev`
- When `@copilot` is assigned an issue, the created branch targets `dev`
- Coordinator can spawn multiple agents with confidence they all target `dev`

## Rationale

### Why Separate dev and main?

1. **Release control** — Explicit `dev` → `main` PR creates a checkpoint for testing and review
2. **Production safety** — `main` is always release-ready; no partial/broken changes
3. **Developer velocity** — `dev` is fast-moving; squad agents can ship frequently without affecting production
4. **Hotfix safety** — Critical fixes bypass `dev` for speed, but must be backported to avoid regression
5. **Industry standard** — Proven pattern (Git Flow variant) used by large teams

### Why GitVersion (SemVer)?

1. **Automatic versioning** — No manual version bumps; derived from git history
2. **Semantic meaning** — MAJOR/MINOR/PATCH signals API compatibility to users
3. **Configuration already exists** — GitVersion.yml at repo root reduces setup overhead
4. **CI integration** — Stamps versions into assemblies automatically
5. **Clear release hygiene** — Version tags linked to git history for reproducibility

### Why 1 Approval for Hotfixes?

1. **Speed** — Critical bugs need fast turnaround; 2 approvals add delay
2. **Gandalf review** — Security-critical hotfixes can tag Gandalf for security review
3. **Main already protected** — All code must pass CI before merge anyway
4. **Backport requirement** — Immediate backport to `dev` catches issues quickly

## Implementation Status

### ✅ Complete (Phase 1-2)

- Created `dev` branch from `main` (identical starting state)
- Applied branch protection rules to both branches
- Updated CI workflow triggers to include `dev` branch
- Updated `squad-issue-assign.yml` to hardcode `dev` as base branch
- Set `dev` as default branch in GitHub settings
- Recorded GitVersion configuration in CI pipeline
- Hotfix backport reminder workflow created (not yet tested in production hotfix scenario)

### 🟡 Testing (Phase 3)

- First `dev` → `main` release PR cycle (verify CI triggers, merge behavior)
- First hotfix scenario (verify backport automation)
- Parallel test execution performance (squad-test.yml with GitVersion)

### 📋 Future Maintenance

1. **Monitor first release cycle** — Verify squash merge behavior and tag creation
2. **If hotfix happens** — Validate backport reminder triggers and process
3. **Adjust CI/CD** if runner time becomes an issue (parallel tests may need optimization)
4. **Document** in contributor guide: "All squad branches target `dev`; only release PRs target `main`"

## Decisions Deferred

- Release PR auto-creation (squad-promote.yml) — manual release PRs for now
- Changelog automation — capture in future sprint
- Release notes on GitHub Release tag — nice-to-have, not MVP

## Related Decisions

- **Pre-push gate** (.squad/decisions.md section below) — Validates code locally before push
- **User secrets** (.squad/decisions.md section below) — Protects Auth0 credentials from commits
- **Casting infrastructure** (.squad/decisions.md section below) — Enables programmatic agent lifecycle management

---

# Decision: Pre-Push Gate — Build and Test Validation

**Date:** 2026-04-18  
**Author:** Boromir (DevOps Engineer)  
**Status:** ✅ Implemented  
**PR:** #12

## Context

Without local validation, developers push broken code to `dev`, triggering CI failures and blocking other team members. The pre-push gate provides immediate feedback (~7 seconds) and reduces CI noise.

## Decision

All developers and squad agents must install a pre-push git hook that validates code before pushing to GitHub.

### Hook Installation

After cloning or creating a new worktree:

```bash
./scripts/install-hooks.sh
```

This installs `.git/hooks/pre-push` which runs before every `git push`.

### Hook Validation Steps

The pre-push hook executes two sequential gates:

1. **Gate 1: Build** — `dotnet build MyBlog.slnx --no-incremental -c Release`
   - Fails on compilation errors or warnings treated as errors

2. **Gate 2: Tests** — `dotnet test MyBlog.slnx --no-build -c Release`
   - Runs Architecture.Tests, Unit.Tests, and Integration.Tests
   - All 3 suites must pass (74 tests minimum)
   - Fails on any test failure

### CI Skip

When `CI=true` environment variable is set, the hook exits immediately (CI environment has already validated).

### Emergency Bypass

In rare cases, bypass validation:

```bash
git push --no-verify
```

⚠️ Use sparingly — CI will still catch issues, but local validation is faster.

### Implementation Details

- `scripts/install-hooks.sh` — Committed source of truth; installs the hook
- `.git/hooks/pre-push` — NOT committed (git doesn't track local hooks); installed by script
- Hook resolves git hooks directory with `git rev-parse --git-path hooks` (supports worktrees)
- Hook backs up any differing existing hook before overwriting

## Rationale

1. **Faster feedback** — Catch broken code in ~7 seconds locally vs 2-5 minutes on CI
2. **Keep CI green** — Reduce failed CI runs, unblock squad agents faster
3. **Developer experience** — Immediate actionable error messages
4. **Team discipline** — Encourages running tests before push; CI is a safety net, not the first check
5. **Worktree-safe** — Uses `git rev-parse --git-path hooks` instead of hardcoded `.git/hooks`

## Impact

- **Push time:** +7 seconds (build + tests) on first push; cached builds faster
- **CI failures:** Expected significant decrease
- **Developer workflow:** One-time setup per clone/worktree (`./scripts/install-hooks.sh`)
- **Squad agents:** Must run install script after branch creation

## Files

- `scripts/install-hooks.sh` — Installation script (committed)
- `.github/hooks/pre-push` — Hook source code (committed)
- `docs/CONTRIBUTING.md` — Documents setup process
- `.github/pull_request_template.md` — Checklist includes pre-push gate setup

## Validation

✅ Tested on PR #12:
- Build: passed (0 errors, 0 warnings)
- Architecture.Tests: ✅ 6/6 passing
- Unit.Tests: ✅ 59/59 passing
- Integration.Tests: ✅ 9/9 passing
- Push: allowed after successful gate validation

## User Directives (Captured 2026-04-18T21:21:06Z)

From mpaulosky (via Copilot):
- **Mandatory:** Pre-push gate is a hard block — retries/blocks push until both `dotnet build` AND `dotnet test` succeed
- **Requirement:** Agents must not push branches until the gate passes
- **Rationale:** Lowering chance of pushing bad code to reduce CI feedback loop

---

# Decision: Auth0 Secrets via User Secrets (Not appsettings)

**Date:** 2026-04-18  
**Author:** Sam (Backend Developer)  
**Status:** Proposed

## Context

The Web app was crashing at startup with `ArgumentException: The value cannot be an empty string (Parameter 'ClientId')` because `appsettings.json` stored empty placeholder strings for Auth0 settings with no user secrets configured.

## Decision

Auth0 credentials (`Auth0:Domain`, `Auth0:ClientId`, `Auth0:ClientSecret`) are **never stored in source-controlled config files**. They must be set via dotnet user-secrets on the Web project:

```bash
dotnet user-secrets set "Auth0:Domain" "<your-tenant>.auth0.com" --project src/Web
dotnet user-secrets set "Auth0:ClientId" "<your-client-id>" --project src/Web
dotnet user-secrets set "Auth0:ClientSecret" "<your-client-secret>" --project src/Web
```

`appsettings.Development.json` documents the required keys (with empty values) as a developer reference. `Program.cs` validates these at startup and throws a clear `InvalidOperationException` with setup instructions if they are missing.

## Rationale

- **Security:** Auth0 secrets must not be committed to source control
- **Developer experience:** Clear error message with instructions beats cryptic middleware exception
- **AppHost design:** Aspire manages infrastructure secrets (MongoDB, Redis), not application-level OAuth credentials
- **Separation of concerns:** Infrastructure (Aspire) vs. application config (user-secrets)

## Consequences

- New developers cloning the repo must run the user-secrets commands before the app will start
- The error message in Program.cs serves as self-documenting setup instructions
- CI/CD will need Auth0 secrets injected as environment variables at deploy time

## Related Decisions

- **Branch strategy** — Ensures secrets are never committed across any branch
- **Pre-push gate** — Catches accidental secrets in local validation before push

---

# Decision: Casting Infrastructure for Agent Lifecycle Management

**Date:** 2026-04-18  
**Author:** Ralph (Infrastructure Specialist)  
**Status:** ✅ Implemented (Phase 1)

## Context

The squad was initialized with `.squad/team.md` roster but lacked the casting infrastructure needed to manage agent lifecycle, policy, and governance decisions programmatically. Ralph created the foundation for deterministic, auditable team management.

## Decision

Establish `.squad/casting/` directory structure with three JSON files:

### 1. `.squad/casting/policy.json` — Governance Defaults

```json
{
  "max_concurrent_agents": 5,
  "default_timeout_minutes": 120,
  "retry_policy": {
    "enabled": true,
    "max_retries": 3,
    "backoff_multiplier": 2,
    "initial_delay_seconds": 5
  },
  "auto_escalate_blockers": true
}
```

**Key settings:**
- `max_concurrent_agents: 5` — Prevents overwhelming system at 11-agent team size
- `default_timeout_minutes: 120` — Allows complex tasks (tests, builds) to complete
- `auto_escalate_blockers: true` — Surfaces blocked work quickly to lead

### 2. `.squad/casting/registry.json` — Team Roster

All 12 agents (11 team members + 1 coordinator) migrated from `.squad/team.md`:
- `legacy_named: true` — No renaming
- `status: "active"` — All operational
- Charter paths point to existing agent directories

### 3. `.squad/casting/history.json` — Migration Audit Trail

Records:
- Timestamp, event type (migration, agent-join, agent-leave, role-change)
- Agent responsible for change
- Detailed impact notes

Establishes pattern for future team changes (onboarding, offboarding, role shifts).

## Rationale

1. **Programmatic control** — Coordinator can spawn agents, manage timeouts, enforce governance via code
2. **Auditability** — Team changes tracked and reversible
3. **Scalability** — Current structure supports growing team without manual intervention
4. **No disruption** — Additive infrastructure; no existing agent behavior changes
5. **Future-proof** — Team size adjustments (>15 agents) can update `max_concurrent_agents` dynamically

## Implementation Status

✅ **Complete:**
- Created `.squad/casting/policy.json` with sensible defaults
- Created `.squad/casting/registry.json` with all agents
- Created `.squad/casting/history.json` with initial snapshot
- Recorded decision rationale in `.squad/decisions/inbox/ralph-casting-migration.md`

🟡 **Next phases (deferred):**
- Phase 2: Agent spawn/timeout automation in coordinator
- Phase 3: Dynamic team scaling based on workload

## Future Maintenance

When team changes:
1. Update `.squad/casting/registry.json` with new agent record or status change
2. Add entry to `.squad/casting/history.json` with timestamp and details
3. Adjust `.squad/casting/policy.json` `max_concurrent_agents` if team size changes significantly (e.g., >15 agents → increase to 7-8)

---

# Decision: PR #11 CSS Artifact Approval

**Date:** 2026-04-18  
**Reviewer:** Legolas (Frontend/Blazor)  
**Status:** ✅ Approved

## Context

PR #11 "chore: commit leftover uncommitted changes" contained a large `src/Web/wwwroot/css/tailwind.css` expansion (minified ~2 lines → pretty-printed 1918 lines). Legolas validated whether this was an intentional compiled artifact or a stale/unwanted asset.

## Decision

✅ **APPROVE** — The CSS expansion is intentional Tailwind v4.2.2 compiled output, not a stale artifact.

## Validation

### What Changed

PR #11 contains:
- `src/Web/wwwroot/css/tailwind.css` — expanded from minified to pretty-printed (1918 lines)
- `src/Web/Features/UserManagement/ManageRoles.razor` — removed redundant `@using MyBlog.Web.Features.UserManagement`

### Why It's Correct

1. **CSS source unchanged** — `src/Web/wwwroot/css/app.css` is identical on dev and PR branch
   - Tailwind v4 CSS-first format confirmed (`@import "tailwindcss"`, `@source` directives, `@theme inline`)
   - No breaking changes to design tokens or component layer

2. **Build recovery, not stale artifact** — PR title indicates code recovery from stale `cicd-phase3-4` branch
   - `npm run tw:build` was never run on cicd-phase3-4 before checkout stalled
   - This PR commits the proper Tailwind compilation that should have been done then
   - Timestamp: tailwind.css touched 2026-04-18 (same as commit date)

3. **Output is valid Tailwind v4.2.2**
   - Header: `/*! tailwindcss v4.2.2 | MIT License | https://tailwindcss.com */`
   - Structure: proper `@layer properties, theme, base, components, utilities`
   - Pretty-printed by default (development format; production uses minified link)

4. **Semantic tokens resolve correctly**
   - All color-primary palettes in `@theme inline` match app.css custom properties
   - Component layer (`.nav-link`, `.btn-primary`, `.card`) present and correct
   - Blazor form validation styles preserved (`.valid.modified`, `.invalid`, `.validation-message`)

5. **No regressions** — CSS file structure aligns with Tailwind v4 migration history
   - All semantic color tokens (theme-blue, theme-red, etc.) properly compiled
   - Dark mode `@custom-variant dark` working correctly
   - Razor component scanning (@source) paths correct for `src/Web/` structure

### Minor Changes

- `ManageRoles.razor`: removed `@using MyBlog.Web.Features.UserManagement` (consistent with 2025-01-29 _Imports.razor consolidation)

## Recommendation

✅ **Merge approved from Blazor/CSS perspective.** No blocker issues.

---

# Decision: PR #12 Pre-Push Gate References

**Date:** 2026-04-18  
**Author:** Boromir (DevOps Engineer)  
**Status:** ✅ Implemented

## Context

PR #12 follow-up review flagged a dead reference to `.squad/playbooks/pre-push-process.md` in SKILL.md. The playbook file doesn't exist.

## Decision

Update SKILL.md and PR template to point to `docs/CONTRIBUTING.md` as the authoritative setup and usage guide instead of referencing a non-existent playbook.

## Rationale

- `docs/CONTRIBUTING.md` already documents hook installation and the five pre-push gates
- Reusing the canonical contributor guide avoids duplicating operational instructions in a second document
- Removing the dead `.squad/playbooks/...` reference keeps the skill accurate for new contributors and agents

## Implementation

✅ **Complete:**
- Updated `.squad/skills/pre-push-test-gate/SKILL.md` to point to `docs/CONTRIBUTING.md`
- Updated `.github/pull_request_template.md` to reference canonical contributor guide
- PR #12 follow-up commit d59b493 pushed with corrections
- All green checks passing

---

# Decision: User Directives on Pre-Push Gate (Captured 2026-04-18)

**From:** mpaulosky (via Copilot)  
**Date:** 2026-04-18T21:21:06Z & 2026-04-18T21:18:50Z

### Directive 1: Pre-Push Gate is Mandatory Hard Block

**What:** The pre-push gate is mandatory and must be a hard block — the gate retries/blocks the push until `dotnet build` AND `dotnet test` both succeed.

**Why:** User request — captured for team memory

**Implementation:** ✅ Complete
- `.github/hooks/pre-push` implements hard block (non-zero exit on failure)
- `scripts/install-hooks.sh` installs the hook
- Emergency bypass documented: `git push --no-verify` (use sparingly)

### Directive 2: Always Run Pre-Push Gate Validation

**What:** Always run a pre-push gate before pushing branches to GitHub. Agents must run `dotnet build` and `dotnet test` locally before `git push` to lower the chance of pushing bad code.

**Why:** User request — captured for team memory

**Implementation:** ✅ Complete
- Pre-push hook enforces `dotnet build MyBlog.slnx -c Release`
- Pre-push hook enforces `dotnet test MyBlog.slnx --no-build -c Release`
- CI skips hook when `CI=true` environment variable set

---

