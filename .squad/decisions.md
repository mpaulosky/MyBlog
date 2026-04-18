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

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
