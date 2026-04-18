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

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
