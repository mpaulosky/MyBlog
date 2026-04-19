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
### 2. Remove Blazor Template Demo Pages (Weather & Counter)

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

### 3. Standardized Copyright Headers for C# Files

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

### 4. CI Workflow Conventions — global.json SDK and Version Stamping

**Date:** 2026-04-18  
**Author:** Boromir (DevOps & CI/CD Engineer)  
**Status:** ✅ Implemented  
**PR:** #9

PR #9 introduced GitVersion integration and parallel test workflows. Copilot review flagged five issues that established important conventions for all future workflow authoring.

#### 4.1 Always use `global-json-file: global.json` in setup-dotnet

When `global.json` is present, use:
```yaml
- uses: actions/setup-dotnet@v4
  with:
    global-json-file: global.json
```
**Never use** `dotnet-version` + `dotnet-quality: 'preview'` when `global.json` exists. The two conflict when `allowPrerelease: false` is set.

#### 4.2 Use `nuGetVersion` for `/p:Version` in dotnet build

```yaml
dotnet build ... /p:Version=${{ steps.gitversion.outputs.nuGetVersion }} /p:InformationalVersion=${{ steps.gitversion.outputs.informationalVersion }}
```
`assemblySemVer` strips prerelease labels. `nuGetVersion` preserves them. Always stamp `InformationalVersion` for full git metadata.

#### 4.3 `squad-test.yml` is PR-only

`squad-test.yml` (parallel tests) must only trigger on `pull_request`. Remove all `push` triggers from it. The `ci.yml` handles push events. Separate responsibilities prevent duplicate CI runs.

#### 4.4 `continue-on-error: true` must be surgical

Only use `continue-on-error: true` on steps that are genuinely optional:
- ✅ PR comment posting (may lack permission in forks)
- ✅ Notification/badge steps
- ❌ Artifact download (if download fails, report is wrong)
- ❌ Coverage generation (if generation fails, data is missing)

#### 4.5 Comments must not contradict code

A comment like `// Get the default branch name (main, master, etc.)` next to `const baseBranch = 'dev'` is actively harmful. Either remove the comment or update it to match the code intent.

**Impact:** Affects all future workflow files in `.github/workflows/`. Any workflow touching .NET setup must use `global-json-file`. Any workflow using GitVersion must use `nuGetVersion`.

### 5. Support Auth0 Role Claim Namespace Variations

**Date:** 2026-04-19  
**Authors:** Frodo (Security), Legolas (Frontend)  
**Status:** ✅ Implemented  

#### Decision

Infer role claim types from the authenticated user's claims when a claim type ends with `role` or `roles`, instead of relying only on the configured namespace list.

#### Rationale

- Auth0 exposes roles under `https://articlesite.com/roles` in production but the app expected `https://myblog/roles`
- Profile rendering and Blazor authorization both depend on role claim recognition
- Supporting both namespaces (and any future variations ending in `role`/`roles`) avoids breaking existing local configuration while branding is being aligned

#### Implementation

- `RoleClaimsHelper` treats namespaced claim types whose tail is `role` or `roles` as role claims
- `appsettings.json` explicitly lists known Auth0 namespaces for reference
- Components (Profile.razor, NavMenu) automatically benefit from role claim normalization

#### Impact

- ✅ Profile card now displays admin role correctly
- ✅ NavMenu admin links appear when user has admin role
- ✅ App is robust to role claim namespace drift
- ✅ `AuthorizeView Roles="..."` directives work across namespace variations

---

### 6. Squad Skills & Playbooks Adoption Review

**Lead:** Aragorn (Lead / Architect)  
**Scope:** Evaluate 19 imported skills and 3 playbooks for MyBlog project adoption  
**Findings Date:** 2026-04-19  
**Status:** Ready for Implementation

#### Executive Summary

The imported skill library is **high-quality and broadly relevant**. Of 19 skills and 3 playbooks reviewed:
- **9 skills are directly useful as-is** — pre-push testing, build repair, MongoDB patterns, CQRS/filtering, Auth0 integration
- **5 skills need light adaptation** — release process (MyBlog-specific binding), Blazor theming (deprecated), testcontainers (test suite alignment)
- **3 skills are inapplicable** — Minecraft world-building, Squad CLI meta (zero-deps), legacy platform-specific tools
- **2 playbooks are production-ready** — pre-push process, PR review gates (enforce our lockout discipline)
- **1 playbook needs extension** — release process (add MyBlog config to generic base)

#### Top 3 Highest-Leverage Adoptions

1. **Pre-Push Test Gate + Build Repair** (skills: `pre-push-test-gate`, `build-repair`)
   - Eliminates broken tests reaching main; gates on zero warnings; mandatory pre-push validation
   - Already documented in existing skills — **ready to enforce immediately**

2. **Testcontainers Shared Fixture Pattern** (skill: `testcontainers-shared-fixture`)
   - Reduces integration test startup from ~46s (per-class) to ~2s (shared containers)
   - Map to MyBlog collections (BlogPosts, Authors, Comments, etc.); add xunit.runner.json parallelization config

3. **MongoDB DBA Patterns + Filter Pattern** (skills: `mongodb-dba-patterns`, `mongodb-filter-pattern`)
   - Formalizes database operations, indexing strategy, backup/recovery, and paginated search
   - Create MongoDB administration runbook; route to Gimli/Sam for repository layer standardization

#### Implementation Roadmap — Phase 1 (Immediate)

| Task | Skill/Playbook | Owner | Effort | Benefit |
|------|---|---|---|---|
| Audit pre-push hook | `pre-push-test-gate` | Aragorn | 30min | Confirm 4 gates active; zero escapes |
| Validate build-repair prompt | `build-repair` | Aragorn | 30min | Ensure zero-warning enforcement |
| Document in CONTRIBUTING.md | `pre-push-process.md` + `pr-merge-process.md` | Frodo + Pippin | 1hr | Team alignment on gates |
| Create MongoDB runbook | `mongodb-dba-patterns` | Sam + Gimli | 2hr | Formalize DBA ops; backup recovery |

---

### 7. DevOps Skills & Playbooks Review for MyBlog

**Author:** Boromir (DevOps)  
**Date:** 2026-04-19  
**Status:** High-Priority Roadmap Ready  
**Tags:** devops, process, automation, quality-gates, ci-cd

#### Executive Summary

MyBlog has **strong process documentation** through playbooks and skills. However, several gaps exist in **local validation automation**, **hook installation enforcement**, and **multi-workspace consistency**.

#### Adopt Now — High Priority (Week 1)

1. **Pre-Push Hook Installation Automation** (2h)
   - Gap: Hook not auto-installed on `git clone`
   - Solution: Add `.git/hooks/post-checkout` that auto-installs pre-push hook
   - Impact: Eliminates new-clone bypass; all developers protected

2. **Docker Availability Check in Pre-Push Gate** (1h)
   - Gap: Hook runs integration tests requiring Docker but doesn't pre-check
   - Solution: Add Gate 0.5: `docker info` check with clear error message
   - Impact: Saves 10+ minutes if Docker is off

3. **CONTRIBUTING.md Pre-Push Section** (1h)
   - Gap: New contributors don't know 5 gates exist
   - Solution: Link playbook from CONTRIBUTING; add quick checklist
   - Impact: Improves discoverability; team alignment

4. **Enforce Squad Branch Naming in Pre-Push Gate** (1h)
   - Gap: Allows pushes to `feature/xyz`, `develop` (non-squad branches)
   - Solution: Tighten Gate 0 to enforce `squad/{issue}-{slug}` regex
   - Impact: Enforces squad conventions locally; prevents routing failures

5. **Merged-PR Branch Guard** (1h)
   - Gap: Agents can commit to merged branches; history gets stranded
   - Solution: Add `.git/hooks/pre-commit` check via `gh pr list`
   - Impact: Detects and blocks merged-branch commits early

#### Adapt First — Medium Priority (Week 2–3)

1. **PR Gate Automation Workflow** (3h)
   - Current: Ralph checks 4 gates manually (Ralph role unassigned)
   - Solution: Create `.github/workflows/pr-gate-check.yml` for automatic validation
   - Impact: Eliminates manual gate checking; 100% consistency

2. **Lightweight MyBlog Release Playbook** (2h)
   - Current: IssueTrackerApp release playbook is project-specific
   - Reality: MyBlog is a blog; single-branch model (main) with no versioning needed
   - Solution: Create `.squad/playbooks/release-myblog.md` (deployment-only)
   - Status: Defer to architecture/release work; flag for Aragorn

#### Top Operational Gaps Closed

| Gap | Problem | Solution | Impact |
|---|---|---|---|
| Broken code reaching CI | Tests pushed before validation | Pre-push hook (Gate 2–4) | Catch 90% locally |
| Wrong files in PRs | Untracked `.cs`/`.razor` invisible to CI | Pre-push Gate 1 warning | Forces developer staging |
| Non-squad branch pushes | Violates reviewer routing | Tighten Gate 0 regex | Enforces conventions |
| Review process inconsistency | Ralph's 4 gates manual; errors slip | GitHub Actions automation | 100% consistency |
| Merged-branch orphaned commits | Agents commit to merged branches | Pre-commit hook guard | Detects early |

---

### 7.1. PR #12 Follow-ups — Pre-Push Gate References

**Date:** 2026-04-19  
**Author:** Boromir (DevOps Engineer)  
**Status:** ✅ Implemented  
**Relates to:** PR #12

#### Decision

The pre-push skill should point contributors to `docs/CONTRIBUTING.md` as the authoritative setup and usage guide instead of referencing a non-existent `.squad/playbooks/pre-push-process.md` playbook.

#### Rationale

- `docs/CONTRIBUTING.md` already documents hook installation and the five pre-push gates.
- Reusing the canonical contributor guide avoids duplicating operational instructions.
- Removes dead `.squad/playbooks/...` reference; keeps skill accurate for new contributors.

---

### 8. Roadmap Rubber-Duck Review — Sprint 0 Complete

**Date:** 2026-04-19  
**Lead:** Aragorn (Lead / Architect)  
**Contributors:** Boromir (DevOps), Coordinator  
**Status:** ✅ Approved — Milestone 1 Work May Begin

#### Decision

The 4-milestone Skills & Playbooks adoption roadmap has been validated against live MyBlog repo state through architectural and operational review. The roadmap is **fundamentally sound** with 5 targeted refinements and 3 execution constraints.

#### Validation Summary

**Architecture Review (Aragorn):**
- ✅ Milestone 0–3 sequence correct; high-leverage wins owned
- ✅ Owner assignments match available capacity and domain expertise
- ✅ Repo fit confirmed for all 9 skills and 2 playbooks
- ⚠️ 5 refinements identified: Sprint splits, pre-flight checklist, effort estimates, release decision logic, deleted-assets manifest
- ⚠️ 3 execution constraints established: Review sign-off gate, pre-push audit, routing PR isolation

**Operational Review (Boromir):**
- ✅ 4 of 5 Milestone 1 items already partly implemented
- ✅ Pre-push hook exists with 5 gates; hook installer exists
- ✅ Contributor docs complete
- 🟡 M1 scope narrowed: 5 items / ~2h (vs. original 4–5h estimate)
- 🟡 Merged-branch guard deferred to M2
- 🟡 Routing injection simpler as quarantine list for M2

#### Key Changes Applied

1. **Split M1 into 1.1 + 1.2:** Boromir (pre-push, 4h) → Aragorn + Pippin (governance, 2h)
2. **Add Sprint 0 Exit Checklist:** Pre-flight gates, skill dispositions, owner capacity, no inbox files
3. **Add Effort Estimates:** All 11 backlog items with P0–P3 sizing
4. **Release Decision Deferred:** M3 acceptance includes explicit logic: "adopt if needed, delete if N/A"
5. **Deleted-Assets Manifest:** M3 acceptance includes `.squad/decisions.md` entry for removed skills

#### Execution Constraints

1. **Constraint 1:** Roadmap review decision logged here (this entry). ✅ Satisfied
2. **Constraint 2:** Boromir audits pre-push hook Gate 1–5 before M1 tightening begins (10 min smoke test)
3. **Constraint 3:** M1b routing PR must not modify `.squad/agents/*/charter.md` or `.squad/decisions/inbox/` files

#### Next Phase

- Boromir: Pre-push audit + M1.1 (Sprint 1.1) implementation
- Aragorn: Milestone 1b + 2–3 roadmap injection into backlog
- Team: Proceed to Milestone 1 with constraints active

---

### 9. Sprint 1.1 — Hook Hardening (Completed)

**Date:** 2026-04-18  
**Author:** Boromir (DevOps / Infra)  
**Status:** ✅ Implemented & Ready for Review  
**Branch:** `squad/1001-sprint-1-1`  
**Commit:** `3e672e6`  
**Related Issue:** Prep for Sprint 1.1 (Milestone 1: Guardrail Adoption)

#### Decision Summary

Sprint 1.1 implements the two planned low-risk guardrail changes to harden the pre-push hook system and enforce squad conventions:

1. **Strict Squad Branch Naming Enforcement** — Gate 0 now validates `squad/{issue}-{slug}` regex
2. **Automatic Hook Bootstrap on Clone** — Post-checkout hook auto-installs pre-push guard on `git clone` and `git checkout`

#### Changes Delivered

1. **Branch Validation Tightening** (`.github/hooks/pre-push`)
   - Before: Only blocked direct pushes to `main`/`dev`
   - After: Requires `squad/{issue}-{slug}` pattern via regex `^squad/[0-9]+-[a-z0-9-]+$`
   - Rationale: Non-squad branches can break routing and reviewer assignment; enforcement is local and pre-push
   - Verification: `squad/1001-sprint-1-1` ✅, `feature/test` ❌ correctly rejected

2. **Auto-Install Hooks on Clone** (`.github/hooks/post-checkout` + `scripts/install-hooks.sh`)
   - Before: Hooks installed only when developers manually ran `./scripts/install-hooks.sh`; new clones silently skipped the pre-push gate
   - After: 
     - New `.github/hooks/post-checkout` hook auto-triggers after every `git clone` and `git checkout`
     - `install-hooks.sh` upgraded to install both pre-push and post-checkout hooks with safe backups
   - Rationale: Human-dependent installation creates reliable bypass; post-checkout automation ensures all developers and CI/CD runners inherit protection
   - Verification: Post-checkout hook executable, auto-bootstrap confirmed, safe backups of existing hooks preserved

#### Pre & Post-Implementation Testing

**Baseline (before changes):** All 5 gates pass cleanly
- Gate 0: Blocks main/dev pushes ✅
- Gate 1: Warns untracked .razor/.cs files ✅
- Gate 2: Release build (0 warnings, 0 errors) ✅
- Gate 3: Unit+Architecture tests (65 passing) ✅
- Gate 4: Integration tests with Docker (9 passing) ✅

**Post-implementation (with new squad-only enforcement):**
- Gate 0: Branch validation ✅ (squad pattern enforced)
- Gate 1–4: All passing ✅
- Non-squad branches correctly rejected ✅
- No blockers identified

#### Known Gotchas & Migration Path

- **Existing branches:** Any branches not matching `squad/{issue}-{slug}` will fail at push with clear guidance (intentional; part of adoption)
- **CI/CD automation:** Branch naming applies to all pushes; automation should use properly named branches or `--no-verify` escape hatch (documented)
- **Worktree safety:** Uses `git rev-parse --show-toplevel` and `--git-path hooks` (safe for worktrees and non-standard Git layouts)

#### Acceptance Checklist

- ✅ Smoke test baseline established (five-gate flow validated)
- ✅ Gate 0 tightened to `squad/{issue}-{slug}` regex
- ✅ Post-checkout hook created and auto-bootstraps pre-push
- ✅ install-hooks.sh handles both pre-push and post-checkout
- ✅ All 5 gates pass on working branch
- ✅ Non-squad branches correctly rejected
- ✅ No merged-branch automation added (deferred to Sprint 1.2+)
- ✅ Minimal docs change (only Gate 0 description updated)
- ✅ Decision documented for team visibility

#### Impact

- **Enforcement:** Squad naming now mandatory locally (not just convention)
- **Reliability:** Hook installation automatic on clone (eliminates bypass path)
- **Discoverability:** Clear error messages guide contributors to fix branch names
- **Adoption:** Prepares foundation for Sprint 1.2 (workflow alignment & docs)

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
