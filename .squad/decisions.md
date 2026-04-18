# Squad Decisions

## Active Decisions

### 1. Consolidate Common @using Directives into _Imports.razor Files

**Date:** 2025-01-29  
**Author:** Legolas (Frontend Developer)  
**Status:** ✅ Implemented & Merged  
**PR:** #4

#### Decision

Consolidate common `@using` directives into the appropriate `_Imports.razor` files while keeping file-specific usings in individual components.

#### Implementation Details

**Features/_Imports.razor** — Added:
- `@using Microsoft.AspNetCore.Authorization`
- `@using MediatR`

**Removed from 9 files:** 14 redundant @using directives

**Criteria for centralization:**
- Appears in 2+ files under same `_Imports.razor` scope
- Represents common framework dependency
- Not tied to specific feature implementation

#### Verification

✅ Build passed (Release config, 0 errors, 0 warnings)  
✅ 76/76 tests passing  
✅ Code review approved  
✅ Main updated to commit 60426b1

#### Impact

- **Code maintainability:** Improved (less duplication)
- **Readability:** Slightly reduced (must reference _Imports for context)
- **Developer experience:** Improved (new pages inherit common usings)
- **Build time:** No change

---
### 1. Remove Blazor Template Demo Pages (Weather & Counter)

**Status:** ✅ Implemented  
**PR:** https://github.com/mpaulosky/MyBlog/pull/6  
**Date:** 2026-04-18

The MyBlog project was initialized from the Blazor Server template, which includes demo pages (Counter and Weather) for learning Blazor. These pages are not relevant to the blog application and have been removed to keep the codebase clean.

**Changes:**
- Deleted `src/Web/Components/Pages/Counter.razor`
- Deleted `src/Web/Components/Pages/Weather.razor`
- Removed 2 template test methods from `tests/Unit.Tests/Components/RazorSmokeTests.cs`

**Impact:**
- 113 lines removed from codebase
- All 74 tests passing (Architecture 6, Unit 59, Integration 9)
- Code coverage: 91.64%
- Cleaner project structure focused on blog functionality

### 2. Standardized Copyright Headers for C# Files

**Status:** ✅ Implemented  
**PR:** https://github.com/mpaulosky/MyBlog/pull/7  
**Date:** 2026-04-18

Adopted standardized 7-line copyright header format for all C# (`.cs`) files in the MyBlog solution.

**Header Format:**
```csharp
//=======================================================
//Copyright (c) {year}. All rights reserved.
//File Name :     {filename}
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  {project}
//=======================================================
```

**Scope:**
- All 46 `.cs` files across 7 projects (AppHost, Domain, ServiceDefaults, Web, Architecture.Tests, Integration.Tests, Unit.Tests)
- Year derived from git log (file creation date)
- Project name auto-detected from directory structure
- Excluded: Razor files, build artifacts

**Rationale:**
1. Legal clarity on copyright ownership and date
2. Professional appearance for code reviews and portfolio
3. Attribution to Matthew Paulosky on every file
4. Consistency across all projects
5. Compliant with charter rule #6

**Implementation:**
- Python script for batch processing (not committed)
- Git log command: `git log --follow --format=%ad --date=format:%Y --diff-filter=A -- {file}`
- All projects build successfully with zero errors and warnings

**Impact:**
- Clear copyright and ownership on every file
- 9 additional lines per file (header + blank line separator)
- Requires maintenance for new files (can be automated)

---

### 3. CI Workflow for Automated PR Validation

**Status:** ✅ Implemented & Merged  
**PR:** #5  
**Date:** 2026-04-18  
**Reviewer:** Gandalf (Security Officer)

**Decision:** Approve and merge `.github/workflows/ci.yml` for PR validation pipeline.

**What Changed:**
- Added `.github/workflows/ci.yml` — full CI pipeline for PR validation
- Workflow triggers on pull_request to main/squad/** and push to main
- Executes: build (Release) + Architecture/Unit/Integration tests + coverage reporting
- Uses GitHub Actions: checkout@v4, setup-dotnet@v4, cache@v4, test-reporter@v1, upload-artifact@v4, CodeCoverageSummary@v1.3.0, sticky-pull-request-comment@v2

**Security Assessment:**
1. ✅ **No hardcoded secrets** — workflow is clean, no credentials in source
2. ✅ **Least-privilege permissions** — `contents:read`, `checks:write`, `pull-requests:write` (minimal)
3. ✅ **Action pinning** — All actions pinned to major versions (@v4, @v1) from trusted publishers (GitHub, dorny, irongut, marocchino)
4. ✅ **No arbitrary code execution** — All commands are static, no eval of user input
5. ✅ **CI environment guard** — Sets `CI=true` to skip Tailwind compilation (appropriate)
6. ✅ **Test isolation** — Separate result directories per suite prevent cross-contamination

**Verification:**
- ✅ Build succeeded (Release config)
- ✅ All 74 tests passing (Arch 6, Unit 59, Integration 9)
- ✅ Code coverage: 91.64%
- ✅ CI checks passed (build-and-test: SUCCESS, Test Results: SUCCESS)

**Impact:**
- Automated validation now active on all future PRs to main and squad/** branches
- Coverage reporting added to PR comments
- Reduced manual security/build review overhead
- Enables coverage tracking and enforcement

**Recommendations for Future PRs:**
1. All PRs to main or squad/** will trigger automated build + test validation
2. PR comments will show code coverage summaries; maintain ≥91%
3. PRs must pass CI checks before merge

---

### 4. Template Cleanup Decision (Gandalf Security Review)

**Status:** ✅ Implemented & Merged  
**PR:** #6  
**Date:** 2026-04-18  
**Reviewer:** Gandalf (Security Officer)

**Decision:** Approve and merge removal of unused demo pages.

**What Changed:**
- Deleted `src/Web/Components/Pages/Counter.razor` (19 lines)
- Deleted `src/Web/Components/Pages/Weather.razor` (66 lines)
- Removed 2 obsolete test methods from `tests/Unit.Tests/Components/RazorSmokeTests.cs` (28 lines)
- Regenerated `src/Web/wwwroot/css/tailwind.css` (minimal diff)
- Total lines removed: 113

**Security Findings:**
1. ✅ **Reduced attack surface** — Removing unused routes (`/counter`, `/weather`) reduces potential attack vectors
2. ✅ **No authorization bypass** — Neither deleted component had `[Authorize]` attributes or role requirements
3. ✅ **Test coverage maintained** — 91.64% line coverage after removing obsolete tests
4. ✅ **No secrets exposed** — No configuration changes, no secret additions or removals

**Verification:**
- ✅ Build succeeded (Release config, 0 errors, 0 warnings)
- ✅ All 74 tests passing (Arch 6, Unit 59, Integration 9)
- ✅ Code coverage: 91.64% maintained

**Impact:**
- Cleaner codebase focused on blog functionality
- Reduced complexity and maintenance burden
- Smaller attack surface
- No security regressions introduced

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
