
## 2025-07-14 — Tailwind Migration Skill Review

Conducted a detailed review of `/home/mpaulosky/.config/squad/.github/skills/tailwind-migration/SKILL.md` against the actual MyBlog project structure.

### Key Learnings

**Project structure confirmed:**
- Source lives under `src/Web/` (not `Web/` as the skill assumes) — every path in the skill is wrong
- Pages follow VSA: `src/Web/Features/BlogPosts/{List,Create,Edit,Delete}/` and `src/Web/Features/UserManagement/`
- No `Counter.razor` or `Weather.razor` — those are default Blazor template pages
- `App.razor` uses Blazor asset fingerprinting: `@Assets["lib/bootstrap/dist/css/bootstrap.min.css"]` — not a plain href
- Bootstrap JS files exist in `wwwroot/lib/bootstrap/dist/js/` but are NOT referenced by `<script>` tags in `App.razor`
- NavMenu uses `<AuthorizeView>` blocks for role-gated links — must be preserved in any rewrite
- `ReconnectModal.razor` + `.razor.css` exist and use Bootstrap classes — not mentioned in skill
- Bootstrap Icons are embedded as inline SVG in `NavMenu.razor.css` background images

**Skill gaps identified (18 findings, 7 Critical):**
- All paths wrong (`./Web/` → `./src/Web/`)
- Reference files (app.css, MainLayout.razor, NavMenu.razor, pages/) do not exist — skill is a stub
- Dynamic theme classes not safelisted → purged in production
- No Blazor form validation CSS (`.valid`, `.invalid`, `.validation-message`)
- Wrong page list (template pages vs actual VSA pages)
- Tailwind v3 vs v4 ambiguity (description says v4+, package.json pins ^3.4.0)
- MSBuild target breaks CI without npm guard
- AuthorizeView guards not mentioned in NavMenu rewrite step

**Priority fixes for skill author:**
1. Fix all paths to `src/Web/`
2. Inline all reference file content directly in SKILL.md
3. Safelist dynamic theme classes in tailwind.config.js
4. Add Blazor validation CSS and preserve AuthorizeView
5. Resolve v3/v4 version ambiguity and fix content glob

**Findings written to:** `.squad/decisions/inbox/aragorn-tailwind-skill-review.md`

## 2026-01-XX — Copyright Header Implementation

Successfully implemented standardized copyright headers across the entire MyBlog solution.

### Key Learnings

**Copyright header format (7-line pattern):**
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

**Implementation details:**
- Applied to all 46 C# files across 7 projects (AppHost, Domain, ServiceDefaults, Web, Architecture.Tests, Integration.Tests, Unit.Tests)
- Year derived from git log first commit (repository shows 2026 due to system time)
- Project name automatically detected from directory structure
- Headers inserted at line 1, followed by blank line before code
- Existing headers (if any) are replaced completely
- All projects build successfully with zero errors and zero warnings

**Process automation:**
- Created Python script to process files in batch
- Git log used to determine file creation year: `git log --follow --format=%ad --date=format:%Y --diff-filter=A -- {file}`
- Project mapping based on directory prefixes (src/Web → Web, tests/Unit.Tests → Unit.Tests, etc.)

**PR created:** https://github.com/mpaulosky/MyBlog/pull/7

**Decision record:** `.squad/decisions/inbox/aragorn-copyright-headers.md`

## 2026-04-19 — Skills & Playbooks Adoption Review

Reviewed 19 imported skills and 3 playbooks from architecture perspective. Findings: 9 directly useful, 5 needing adaptation, 5 low-value.

**Top 3 Adoptions:**
1. Pre-Push Test Gate + Build Repair — ready to enforce immediately
2. Testcontainers Shared Fixture — reduce integration test startup 46s → 2s
3. MongoDB DBA Patterns — formalize governance, indexing, query standards

**Key Decisions:**
- Audit pre-push hook (30min) — confirm 4 gates active
- Finalize release playbook binding with Boromir (3h) — MyBlog-specific variant
- Route MongoDB work to Gimli/Sam with filter-pattern injection

**Outcome:** Decision merged to decisions.md (section 6). Ready for Phase 1 implementation (immediate).

## 2026-04-19: Roadmap Rubber-Duck Review (Sprint 0)

Led architectural validation of 4-milestone Skills & Playbooks adoption roadmap. Approved with 5 targeted changes and 3 execution constraints. Key findings:
- Milestone sequence correct; ownership appropriate
- Identified need for Sprint 1 split (1.1 pre-push tightening + 1.2 governance)
- Added pre-flight checklist, effort estimates, release decision logic, deleted-assets manifest
- Execution constraints: review sign-off gate, pre-push audit, routing PR isolation
- Next: Monitor M1 implementation with constraints active

Decision logged: `.squad/decisions.md` entry #8

## 2026-04-19: Sprint 1.2 Completion — Route Process Skills Into Workflow

Completed Milestone 1b (Sprint 1.2) by embedding guardrails skills into normal squad routing.

**Decision 12: Route Process Skills Into Normal Squad Workflow**

Updated `.squad/routing.md` to make guardrails explicit at every handoff:

1. **Skills Injection Rules (refined):**
   - Pre-push gate: "Any push-capable work"
   - Build repair: "When build/test health is red"
   - PR merge playbook: "When PR review starts"
   - Merged-PR guard: "Before committing to old squad branches"

2. **Workflow Guardrails (5 numbered rules):**
   - Before push-ready handoff → pre-push gate + playbook
   - Build/test red → build repair first (not normal feature work)
   - PR work → PR merge playbook as checklist
   - Old squad branch → merged-PR guard before commit
   - No quarantined imports (building-protection stays excluded)

3. **Quarantine Clarity:**
   - Explicitly marked `building-protection` as do-NOT-inject
   - Prevents accidental reuse of Minecraft skill pending M3 disposition

**Impact:** Future coordinators now have explicit routing rules for guardrails adoption. Push-capable work, build repair, PR gates, and branch safety all automatically injected at the right moments.

**Files Modified:**
- `.squad/routing.md` — Skills section extended; Workflow Guardrails section clarified

**Timeline:** Completed as part of coordinated M1.2 effort with Pippin.

**Constraints Satisfied:**
- ✅ Roadmap review decision logged (section 8, decisions.md)
- ✅ Boromir pre-push audit completed (Sprint 1.1)
- ✅ M1.2 routing PR does not modify agent charters or inbox

**Outcome:** ✅ Routing table now fully describes post-S1.1 workflow with explicit guardrails at every step.

## 2026-04-19: Milestone 3 Roadmap Completion (Final)

**Milestone:** 3 (Adapt-or-Delete Cleanup & Roadmap Completion)  
**Outcome:** ✅ Complete

Finalized all remaining roadmap decisions for Milestone 3 to enable sprint 3 cleanup execution.

### Key Achievements

1. **Release Guidance Finalized (Decision #13)**
   - Confirmed MyBlog-specific release routing (skills/release-process → playbooks/release-myblog)
   - Approved deletion of generic release-process-base template (replaced by repo-specific guidance)
   - Clarified branch model: `dev` → `main` (no back-sync); hotfixes backport to `dev` only
   - Release ownership: Aragorn scope approval → Boromir operational execution

2. **Asset Disposition Approved (Decision #14)**
   - Approved deletion of post-build-validation, static-config-pattern, building-protection, release-process-base
   - Confirmed microsoft-code-reference retention (rewrite queued, Boromir backlog)
   - Delegated manifest publication to Pippin (DELETED-ASSETS.md)

### Cross-Team Coordination

- **Coordinated with Boromir:** Merged-branch guard decision (Decision #12) — keep guidance-only, defer automation pending incident frequency
- **Coordinated with Pippin:** Published DELETED-ASSETS.md manifest as authoritative reference for future contributors
- **Routed with Scribe:** All three decisions consolidated to decisions.md; inbox merged; agent history cross-linked

### Modified Assets

- Decision merged: Decision #13 (Release guidance fit) → `.squad/decisions.md`
- Decision merged: Decision #14 (Delete non-fit assets) → `.squad/decisions.md`
- Orchestration logged: `2026-04-19T04-04-30-aragorn-sprint-3-roadmap.md`

### Roadmap Impact

- Milestone 3 "Adapt-or-Delete" pass now complete
- Release work scope & ownership crystal clear
- Sprint 3 cleanup can proceed with full decision context
- No misleading generic guidance remains in routing layer

**Constraints Satisfied:**
- ✅ Release guidance anchored to real `dev`/`main`/`hotfix` workflow  
- ✅ All imports explicitly marked adapt/delete/retain  
- ✅ Decisions logged with structured rationale  
- ✅ Cross-team coordination documented  
