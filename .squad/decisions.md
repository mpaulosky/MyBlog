# Squad Decisions

## Active Decisions

### 1. Versioning Strategy: Sprint Tags → Pure Semantic Versioning

**Status:** ✅ Decided  
**Date:** 2026-04-20  
**Decided by:** User + Coordinator

Switch from sprint-based release tags (`v1.0.0-sprint3`) to pure semantic versioning (`v1.0.1`, `v1.0.2`, ...) with GitVersion automation in CI/CD.

**Rationale:**

- GitVersion is already running in `.github/workflows/ci.yml` (active since Sprint 3 setup)
- Sprint tags impose manual friction (guessing next tag, remembering conventions)
- Pure semver decouples version from scope — cleaner for production releases
- Main branch configured to increment Patch on each commit (GitVersion.yml verified)
- Backward compatible: `v1.0.0-sprint3` remains the final sprint tag; subsequent releases start at `v1.0.1`

**Implementation:**

- No code changes needed — GitVersion config already correct
- Release workflow: merge PR → CI auto-tags with semver → Aragorn creates GitHub release from tag
- Discipline: Aragorn ceases manual `-sprint#` tagging; CI handles versioning deterministically
- First post-sprint release: v1.0.1 (marks sprint-to-semver cutover)

**Decisions Impacted:**

- Release process documentation updated
- Team no longer manually tracks `-sprint#` suffixes
- Version bumping fully automated via GitVersion

### 2. Remove Blazor Template Demo Pages (Weather & Counter)

**Status:** ✅ Implemented  
**PR:** <https://github.com/mpaulosky/MyBlog/pull/6>  
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
**PR:** <https://github.com/mpaulosky/MyBlog/pull/7>  
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

### 3. CI Workflow Conventions — global.json SDK and Version Stamping

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

### 4. Support Auth0 Role Claim Namespace Variations

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

### 5. Squad Skills & Playbooks Adoption Review

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

| Task                         | Skill/Playbook                                | Owner          | Effort | Benefit                              |
| ---------------------------- | --------------------------------------------- | -------------- | ------ | ------------------------------------ |
| Audit pre-push hook          | `pre-push-test-gate`                          | Aragorn        | 30min  | Confirm 4 gates active; zero escapes |
| Validate build-repair prompt | `build-repair`                                | Aragorn        | 30min  | Ensure zero-warning enforcement      |
| Document in CONTRIBUTING.md  | `pre-push-process.md` + `pr-merge-process.md` | Frodo + Pippin | 1hr    | Team alignment on gates              |
| Create MongoDB runbook       | `mongodb-dba-patterns`                        | Sam + Gimli    | 2hr    | Formalize DBA ops; backup recovery   |
| Task | Skill/Playbook | Owner | Effort | Benefit |
| ------ | --- | --- | --- | --- |
| Audit pre-push hook | `pre-push-test-gate` | Aragorn | 30min | Confirm 4 gates active; zero escapes |
| Validate build-repair prompt | `build-repair` | Aragorn | 30min | Ensure zero-warning enforcement |
| Document in CONTRIBUTING.md | `pre-push-process.md` + `pr-merge-process.md` | Frodo + Pippin | 1hr | Team alignment on gates |
| Create MongoDB runbook | `mongodb-dba-patterns` | Sam + Gimli | 2hr | Formalize DBA ops; backup recovery |

---

### 7. DevOps Skills & Playbooks Review for MyBlog

### 6. DevOps Skills & Playbooks Review for MyBlog

**Author:** Boromir (DevOps)  
**Date:** 2026-04-19  
**Status:** High-Priority Roadmap Ready  
**Tags:** devops, process, automation, quality-gates, ci-cd

#### Executive Summary 1

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

| Gap                            | Problem                                  | Solution                  | Impact                   |
| ------------------------------ | ---------------------------------------- | ------------------------- | ------------------------ |
| Broken code reaching CI        | Tests pushed before validation           | Pre-push hook (Gate 2–4)  | Catch 90% locally        |
| Wrong files in PRs             | Untracked `.cs`/`.razor` invisible to CI | Pre-push Gate 1 warning   | Forces developer staging |
| Non-squad branch pushes        | Violates reviewer routing                | Tighten Gate 0 regex      | Enforces conventions     |
| Review process inconsistency   | Ralph's 4 gates manual; errors slip      | GitHub Actions automation | 100% consistency         |
| Merged-branch orphaned commits | Agents commit to merged branches         | Pre-commit hook guard     | Detects early            |
| Gap | Problem | Solution | Impact |
| --- | --- | --- | --- |
| Broken code reaching CI | Tests pushed before validation | Pre-push hook (Gate 2–4) | Catch 90% locally |
| Wrong files in PRs | Untracked `.cs`/`.razor` invisible to CI | Pre-push Gate 1 warning | Forces developer staging |
| Non-squad branch pushes | Violates reviewer routing | Tighten Gate 0 regex | Enforces conventions |
| Review process inconsistency | Ralph's 4 gates manual; errors slip | GitHub Actions automation | 100% consistency |
| Merged-branch orphaned commits | Agents commit to merged branches | Pre-commit hook guard | Detects early |

---

### 7.1. PR #12 Follow-ups — Pre-Push Gate References

### 6.1. PR #12 Follow-ups — Pre-Push Gate References

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

### 7. Roadmap Rubber-Duck Review — Sprint 0 Complete

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

### 8. Sprint 1.1 — Hook Hardening (Completed)

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

### 9. Document Guardrails Update (Sprint 1.2)

**Date:** 2026-04-19  
**Owner:** Pippin (Docs)  
**Status:** ✅ Implemented  
**Related:** Sprint 1.2 / Milestone 1b

Updated `docs/CONTRIBUTING.md` to accurately reflect the enforced workflow after Sprint 1.1 hook hardening.

#### Changes Made

1. **Branch Naming Enforcement (Gate 0)**
   - Clarified that the pre-push hook now strictly enforces `squad/{issue}-{slug}` branch naming.
   - Added examples: `squad/42-fix-login-validation`, `squad/103-add-blog-search-feature`.
   - Removed outdated language about merely blocking `main`/`dev` pushes.

2. **Gate Documentation Accuracy**
   - Gate 0: Now describes strict `squad/*` naming + `main`/`dev` block (not just the latter).
   - Gate 1–4: Confirmed and clarified exact test projects, Docker requirement, and retry behavior.
   - Added emphasis on 3 retry attempts for Gates 2–4.

3. **Hook Installation**
   - Updated to reflect that hooks are now installed **automatically** on clone via `post-checkout`.
   - Noted that manual reinstall via `./scripts/install-hooks.sh` is available if needed.
   - Removed outdated language suggesting manual installation was required.

4. **PR Workflow Alignment**
   - Added explicit reference to playbooks: `.squad/playbooks/pre-push-process.md` and `.squad/playbooks/pr-merge-process.md`.
   - Clarified PR creation, CI wait gate, and squash-merge flow.
   - Added note about automatic branch cleanup and manual cleanup commands.

5. **Troubleshooting Section**
   - Added concrete troubleshooting subsections for build, test, and Docker failures.
   - Cross-referenced naming conventions and DateTime assertion patterns from pre-push playbook.

#### Rationale

Contributors follow `docs/CONTRIBUTING.md` for onboarding and workflow. If the doc does not match the enforced workflow, new team members will be confused, leading to failed local pushes, wasted CI cycles, and support burden.

By aligning the doc with the actual enforced workflow (post-Sprint 1.1 hardening):

- **Reduce onboarding friction:** New contributors see the actual rules, not aspirational docs.
- **Improve success rate:** Contributors understand branch naming and hook behavior upfront.
- **Enable self-service troubleshooting:** Troubleshooting section mirrors playbook guidance.
- **Provide clear escalation paths:** Links to playbooks allow deeper investigation without asking maintainers.

#### Impact

- ✅ CONTRIBUTING.md now matches Sprint 1.1 enforced workflow
- ✅ All internal links verified (relative paths from /docs/)
- ✅ No governance files modified

---

### 10. Merged-Branch Awareness Guidance (Sprint 1.2)

**Decided:** Pippin (Docs)  
**Date:** 2026-04-19  
**Status:** ✅ Implemented  
**Sprint:** 1.2 / Milestone 1b

Added a lightweight, contributor-facing section to `docs/CONTRIBUTING.md` that warns about committing on already-merged branches and provides a safe recovery path.

#### Decision

Add guidance to `docs/CONTRIBUTING.md` warning contributors not to commit on merged branches and documenting safe recovery path, without claiming automation enforcement.

#### Rationale

- **Risk:** Contributors sometimes continue work on `squad/*` branches after their PR has been merged. This creates orphaned commits that diverge from main and clutter the branch history.
- **Gap:** Current docs did not explicitly warn against this or provide a safe path.
- **Approach:** Document the risk and recovery path now (Sprint 1.2) as a lightweight quarantine. Defer heavier automation (e.g., a pre-commit guard) until Sprint 2 repo-fit review determines whether the frequency justifies the complexity.

#### What Changed

Added **"After Your PR Is Merged"** subsection under "Pull Requests" in `docs/CONTRIBUTING.md` that:

1. **Warns** contributors not to commit on merged branches
2. **Guides recovery:** `git checkout dev`, `git pull origin dev`, then create a fresh `squad/{issue}-{slug}` branch
3. **Explains why:** "New commits on a merged branch create orphaned history; starting fresh on a new issue branch keeps the repository clean"
4. **Does NOT claim automation:** guidance is voluntary, not enforced by hook or CI

#### Related Artifacts

- **CONTRIBUTING.md** — Updated with merged-branch awareness section
- **Merged-PR Guard Skill** — `.squad/skills/merged-pr-guard/SKILL.md` (higher-level automation deferred)

#### Future

After Sprint 2 repo-fit review, the team may decide to:

- Implement a pre-commit guard if the frequency warrants automation
- Keep the guidance-only approach if contributors naturally avoid the anti-pattern
- Escalate to a stricter hook if orphaned branches remain a problem

This decision does not block either path; it just documents the interim lightweight quarantine.

#### Impact

- ✅ Contributors now have explicit guidance and recovery path
- ✅ Voluntary (not enforced); allows team to measure frequency before automating
- ✅ Defers higher-friction automation until justified by data

---

### 11. Route Process Skills Into Normal Squad Workflow

**Author:** Aragorn (Lead / Architect)  
**Date:** 2026-04-19  
**Status:** ✅ Implemented  
**Sprint:** 1.2 / Milestone 1b

MyBlog's adopted process guardrails now belong in normal squad routing, not just in standalone skills and playbooks. Coordinators should inject the pre-push, build-repair, merged-branch, and PR-merge assets whenever work reaches those states.

`building-protection` remains explicitly quarantined. It is still a Minecraft skill and should not be treated as part of MyBlog's working process.

#### Decision

Embed guardrails skills (pre-push, build-repair, merged-branch, PR-merge) into `.squad/routing.md` as formal routing rules so that future squad work automatically receives these assets at the right handoff points.

#### Rationale

Sprint 1.1 already hardened the live workflow by enforcing `squad/{issue}-{slug}` branches and auto-installing hooks. Routing now needs to surface the same guardrails so future squad work follows the real MyBlog delivery path by default instead of relying on ad hoc memory.

Keeping `building-protection` in the routing table as a do-not-inject asset is intentional. It prevents accidental reuse of a non-fit imported skill while the later adapt-or-delete pass is still pending.

#### Changes Made

**Updated `.squad/routing.md` — Skills section:**

| Domain                                    | Asset                                                                                | When to Inject                                                                                                                                |
| ----------------------------------------- | ------------------------------------------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------- |
| Push-capable squad work                   | `.squad/skills/pre-push-test-gate/SKILL.md` + `.squad/playbooks/pre-push-process.md` | Any task expected to end in `git push`, branch handoff, or local gate validation. Default for normal `squad/{issue}-{slug}` delivery.         |
| Build/test gate failures                  | `.squad/skills/build-repair/SKILL.md` + `.squad/skills/pre-push-test-gate/SKILL.md`  | Any task blocked by Release build failures, warning cleanup, failing tests, or a rejected pre-push gates run. Aragorn owns this route.        |
| PR review, approval, merge                | `.squad/playbooks/pr-merge-process.md`                                               | Any Aragorn-led PR gate once CI is green, including Copilot-review read, parallel reviewer fan-out, merge, and cleanup.                       |
| Resumed work on existing `squad/*` branch | `.squad/skills/merged-pr-guard/SKILL.md`                                             | Any agent about to `git commit` on a branch with prior PR activity or uncertain session state. Check for already-merged PR before committing. |
| Domain | Asset | When to Inject |
| -------- | ------- | ---------------- |
| Push-capable squad work | `.squad/skills/pre-push-test-gate/SKILL.md` + `.squad/playbooks/pre-push-process.md` | Any task expected to end in `git push`, branch handoff, or local gate validation. Default for normal `squad/{issue}-{slug}` delivery. |
| Build/test gate failures | `.squad/skills/build-repair/SKILL.md` + `.squad/skills/pre-push-test-gate/SKILL.md` | Any task blocked by Release build failures, warning cleanup, failing tests, or a rejected pre-push gates run. Aragorn owns this route. |
| PR review, approval, merge | `.squad/playbooks/pr-merge-process.md` | Any Aragorn-led PR gate once CI is green, including Copilot-review read, parallel reviewer fan-out, merge, and cleanup. |
| Resumed work on existing `squad/*` branch | `.squad/skills/merged-pr-guard/SKILL.md` | Any agent about to `git commit` on a branch with prior PR activity or uncertain session state. Check for already-merged PR before committing. |

**Updated `.squad/routing.md` — Workflow Guardrails section:**

Added 5 numbered workflow rules that clarify when/how guardrails apply post-Sprint 1.1:

1. Before any push-ready handoff, route through the pre-push gate skill and playbook
2. When build/test health is red, route through build repair first (not normal feature work)
3. When PR work starts, use PR merge playbook as governing checklist
4. When resuming a squad branch, apply the merged-PR guard before committing
5. Do not normalize quarantined imports (e.g., building-protection stays out until M3 disposition)

#### Impact

- ✅ Push-capable work now points to the same pre-push guardrails every time
- ✅ Build/test repair work is explicitly escalated instead of hidden inside normal implementation
- ✅ PR review and merge flow stay tied to Aragorn's gatekeeping process
- ✅ Quarantined imported skills remain visible without becoming part of normal MyBlog execution
- ✅ Future coordinators have explicit routing rules for guardrails adoption

---
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

---

## Milestone 2: Skill Mining & Repo Adaptation (2026-04-19)

### A. Auth0 Skills Mining (Frodo)

**Status:** ✅ Merged to decisions  
**Decision Type:** Skill retention & adaptation  
**Modified:** `.squad/skills/auth0-management-api/SKILL.md`, `.squad/skills/auth0-management-security/SKILL.md`

#### Summary

The Auth0 Management API and Security skills have been mined from imported reusable patterns into MyBlog-specific guidance. Both skills are **retained and adapted** because:

1. Auth0 is production infrastructure in this repo (active M2M app, role-based access control)
2. Real operational patterns exist (UserManagementHandler, RoleClaimsHelper, Management API integration)
3. Security-critical content (secrets, authorization, error handling) benefits the team

#### Key Adaptations

- Narrowed scope to MyBlog usage only; removed generic "best practices" sections
- Bound to real code paths: UserManagementHandler, RoleClaimsHelper, Program.cs
- Auth0 Management API v7.46.0 mapped to actual M2M app scopes
- Called out future work: caching layer, audit logging (backlog items)
- Authorization: AdminPolicy guards all /admin/users routes

#### Ownership & Routing

| Asset                       | Owner               | Primary Audience                           | Trigger                                                   |
| --------------------------- | ------------------- | ------------------------------------------ | --------------------------------------------------------- |
| `auth0-management-api`      | Frodo (Tech Writer) | All squad; particularly Legolas (Frontend) | Role operations, API integration review                   |
| `auth0-management-security` | Frodo (Tech Writer) | All squad                                  | Security audit, secrets review, auth configuration change |
| `docs/AUTH0_SETUP.md`       | Frodo (Tech Writer) | Onboarding, new developers                 | Initial repo setup                                        |
| Asset | Owner | Primary Audience | Trigger |
| --- | --- | --- | --- |
| `auth0-management-api` | Frodo (Tech Writer) | All squad; particularly Legolas (Frontend) | Role operations, API integration review |
| `auth0-management-security` | Frodo (Tech Writer) | All squad | Security audit, secrets review, auth configuration change |
| `docs/AUTH0_SETUP.md` | Frodo (Tech Writer) | Onboarding, new developers | Initial repo setup |

#### Next Steps

- Frodo: Add Auth0 secrets policy to SECURITY.md (Sprint 2)
- Gimli/Sam: Extract reusable test patterns for testing-patterns skill (Sprint 2 test review)
- All squad: Review adapted skills for feedback

---

### B. MongoDB Skills Mining (Sam)

**Status:** ✅ Merged to decisions  
**Decision Type:** Skill retention & adaptation  
**Modified:** `.squad/skills/mongodb-dba-patterns/SKILL.md`, `.squad/skills/mongodb-filter-pattern/SKILL.md`

#### Summary

The imported `mongodb-dba-patterns` and `mongodb-filter-pattern` skills have been rewritten to describe MyBlog's actual Mongo stack instead of generic MongoDB guidance.

#### MyBlog MongoDB Baseline

- **Runtime wiring:** `src/AppHost/AppHost.cs` creates `mongodb` and database `myblog`; `src/Web/Program.cs` consumes via Aspire
- **Persistence contract:** `src/Web/Data/BlogDbContext.cs` maps `BlogPost` to `blogposts` with `Version` as concurrency token
- **Repository path:** `IBlogPostRepository` + `MongoDbBlogPostRepository` abstracts backend, returns domain entities
- **Read path:** `GetBlogPostsHandler` owns DTO mapping and caching
- **Verification path:** `MongoDbFixture` and `MongoDbBlogPostRepositoryTests` are canonical proof paths

#### Retained Adaptations

- **`mongodb-dba-patterns`** — Bound to real owners and code paths:
  - Sam: repository, mapping, query/index implications
  - Gimli: Mongo integration verification
  - Boromir: environment rollout, shared backups/upgrades
  - Frodo: secrets, TLS, least privilege for non-local deployments

- **`mongodb-filter-pattern`** — Rebased on actual read pipeline:
  - GetBlogPostsQuery → GetBlogPostsHandler → IBlogPostRepository → MongoDbBlogPostRepository
  - Replaced driver filter examples with EF Core LINQ guidance
  - Cache-key expansion is first-class rule (list reads are handler-cached)
  - Repositories return domain entities; handlers own `Result<T>` wrapping

#### Non-Fit Items Explicitly Called Out

- Manual replica-set bootstrap commands (not part of local dev)
- Atlas-only cluster administration (future-only if shared deployment adopted)
- `Builders<T>.Filter` / `BsonRegularExpression` examples (not MyBlog's default path)
- Minimal API and HTTP-client query-string patterns (current stack uses Blazor + MediatR)

#### Follow-up Guidance

1. Keep future Mongo guidance anchored to actual files in `src/Web`, `src/AppHost`, `tests/Integration.Tests`
2. If MyBlog adopts shared Mongo infrastructure, extend DBA skill with environment-specific instructions
3. If MyBlog adopts REST list endpoints or FluentValidation, revisit filter skill instead of silently reviving deleted sections
4. During Milestone 3 cleanup, review whether future-only DBA sections still earn their place

---

### C. Testing Patterns Adaptation (Gimli)

**Status:** ✅ Merged to decisions  
**Decision Type:** Skill retention, adaptation, & repo conventions  
**Modified:** `.squad/skills/testcontainers-shared-fixture/SKILL.md`, `.squad/skills/webapp-testing/SKILL.md`, tests/Integration.Tests suite files

#### Summary

Imported `testcontainers-shared-fixture` and `webapp-testing` skills have been adapted to MyBlog's real test layout:

- `Architecture.Tests` (architecture rule enforcement)
- `Unit.Tests` (unit tests + bUnit components)
- `Integration.Tests` (Mongo-backed integration tests via Testcontainers)

#### Current MyBlog Testing Baseline

- Automated suite: `dotnet test MyBlog.slnx --configuration Release`
- Architecture tests: `tests/Architecture.Tests`
- Unit + bUnit tests: `tests/Unit.Tests`
- Mongo integration tests: `tests/Integration.Tests`
- Live Mongo fixture: `tests/Integration.Tests/Infrastructure/MongoDbFixture.cs`
- Browser-free UI coverage: `NavMenuTests`, `ProfileTests`, `RazorSmokeTests`

#### Retained Adaptations

- **`testcontainers-shared-fixture`** — Adapted to MyBlog integration suite:
  - Domain collections: `BlogPostIntegration`, `AuthorIntegration`, etc.
  - Per-test database names: `$"T{Guid.NewGuid():N}"`
  - Collection-level parallelism: only approved xUnit mode for Mongo-backed work
  - Concrete convention: `MongoDbBlogPostRepositoryTests` uses `BlogPostIntegration` collection

- **`webapp-testing`** — Retained and narrowed:
  - Browser testing positioned as manual/runtime verification aid (not committed test framework)
  - Contributors start with bUnit; use browser tooling only for runtime-only checks
  - AppHost is preferred launch path for infrastructure-aware smoke verification
  - JS theme interop, Auth0 redirect wiring, AppHost smoke behavior noted as runtime-only checks

#### Explicit Non-Fit Items

- Source-repo collection mappings don't fit MyBlog's current test inventory
- Source-repo performance numbers not reused (not measured against MyBlog)
- Fixture-only `GlobalUsings.cs` recommendation premature for current suite size
- Automatic Playwright/Node setup **not** a MyBlog convention
- Committed browser-spec projects, CI browser lanes, generic form/responsive sweeps **not** part of current repo
- `tests/Integration.Tests/IntegrationTest1.cs` remains generic scaffold (pending team decision on AppHost smoke tests)

#### Conventions Baked Into Repo

- `tests/Integration.Tests/BlogPosts/MongoDbBlogPostRepositoryTests.cs` — uses domain-specific collection
- `tests/Integration.Tests/Infrastructure/BlogPostIntegrationCollection.cs` — created as collection definition
- `tests/Integration.Tests/xunit.runner.json` — establishes collection-level parallel rules
- `tests/Integration.Tests/Integration.Tests.csproj` — verified configuration

#### Follow-up

1. Replace or delete `IntegrationTest1.cs` once team decides on AppHost smoke tests
2. If browser-only regressions become common, open separate backlog for dedicated E2E project

---

### D. Secondary Skills Assessment (Boromir)

**Status:** ✅ Merged to decisions  
**Decision Type:** Secondary skill fit assessment  
**Assets:** post-build-validation, static-config-pattern, microsoft-code-reference

#### Summary

Three secondary imported skills assessed against MyBlog's repository structure, build/test workflows, configuration practices, and .NET Aspire architecture.

#### Assessment Results

| Skill                        | Fit        | Decision             | Reason                                                                                                                                                                                                                                                                                       |
| ---------------------------- | ---------- | -------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **post-build-validation**    | ❌ Poor     | **DELETE**           | Pattern designed for external game-world state validation (RCON block verification after structure placement). MyBlog has no remote operations, RCON commands, or external API validation. No scenario for graceful degradation on validation failure. Test failures **should** block build. |
| **static-config-pattern**    | 🟡 Marginal | **DELETE**           | Backwards-compatible const→static property refactor. MyBlog already uses ASP.NET Core `IConfiguration` + Options pattern. Const fields (`health path`, `cache key`) are infrastructure internals, not legacy config debt. No current business case.                                          |
| **microsoft-code-reference** | ✅ Good     | **RETAIN & CLARIFY** | Reference skill (tools + query patterns), not code pattern. Applicable during CI/CD troubleshooting, NuGet verification, Azure SDK method lookup, GitHub Actions pattern discovery. Needs rewrite to clarify scope for DevOps/NuGet/GitHub Actions scenarios.                                |
| Skill | Fit | Decision | Reason |
| ------- | ----- | ---------- | -------- |
| **post-build-validation** | ❌ Poor | **DELETE** | Pattern designed for external game-world state validation (RCON block verification after structure placement). MyBlog has no remote operations, RCON commands, or external API validation. No scenario for graceful degradation on validation failure. Test failures **should** block build. |
| **static-config-pattern** | 🟡 Marginal | **DELETE** | Backwards-compatible const→static property refactor. MyBlog already uses ASP.NET Core `IConfiguration` + Options pattern. Const fields (`health path`, `cache key`) are infrastructure internals, not legacy config debt. No current business case. |
| **microsoft-code-reference** | ✅ Good | **RETAIN & CLARIFY** | Reference skill (tools + query patterns), not code pattern. Applicable during CI/CD troubleshooting, NuGet verification, Azure SDK method lookup, GitHub Actions pattern discovery. Needs rewrite to clarify scope for DevOps/NuGet/GitHub Actions scenarios. |

#### MyBlog Context

- **Build process:** `ci.yml` runs build, then three sequential test suites (Architecture, Unit, Integration)
  - No external remote operations or out-of-band state verification
  - All validation is in-process (xUnit assertions + TestContainers)
  - Test failures **must** block the build

- **Configuration approach:** MyBlog uses ASP.NET Core standard `IConfiguration` + Options pattern
  - Health endpoint path is a const in ServiceDefaults (infrastructure internal)
  - Cache key is a const in GetBlogPostsHandler (infrastructure internal)
  - No consts intended to be runtime-configurable

#### Disposition Timeline

| Item                     | Action                                          | Owner   | Effort | Sprint             |
| ------------------------ | ----------------------------------------------- | ------- | ------ | ------------------ |
| post-build-validation    | Delete from `.squad/skills/`                    | Boromir | 5 min  | Sprint 3           |
| static-config-pattern    | Delete from `.squad/skills/`                    | Boromir | 5 min  | Sprint 3           |
| Item | Action | Owner | Effort | Sprint |
| ------ | -------- | ------- | -------- | -------- |
| post-build-validation | Delete from `.squad/skills/` | Boromir | 5 min | Sprint 3 |
| static-config-pattern | Delete from `.squad/skills/` | Boromir | 5 min | Sprint 3 |
| microsoft-code-reference | Rewrite for DevOps/NuGet/GitHub Actions, retain | Boromir | 30 min | Sprint 2 (backlog) |

#### Implementation Notes

1. **Immediate (Sprint 2 backlog):** Update backlog item #10 to note microsoft-code-reference scope clarification
2. **Sprint 2:** Queue rewrite of microsoft-code-reference for DevOps use (NuGet verification, Azure SDK, GitHub Actions)
3. **Sprint 3:** Remove `post-build-validation/` and `static-config-pattern/` directories; add entries to deletion manifest

---

## Governance Update (Milestone 2)

All meaningful skill retention decisions now include:

1. Explicit ownership & routing rules
2. Anchoring to real MyBlog code paths (no generic guidance)
3. Called-out future work (backlog items, not current implementation)
4. Clear non-fit items (what imported content does NOT apply)
5. Follow-up guidance for Stack changes or new requirements

---

## Milestone 3 Decisions (Roadmap Completion & Adapt-or-Delete Cleanup)

### 12. Merged-Branch Guard — Keep Guidance-Only, Defer Automation

**Date:** 2026-04-19  
**Owner:** Boromir (DevOps)  
**Status:** ✅ Final Decision  
**Milestone:** 3 (Adapt-or-Delete Cleanup)  
**Related Issue:** Sprint 2 backlog item #11

#### Context

The plan (`.squad/identity/now.md`, Milestone 2 Sprint 2, item 11) asked: "Revisit whether merged-branch automation is still justified after Sprint 1 awareness, and only implement it if the repo-fit review still supports it."

The imported skill `.squad/skills/merged-pr-guard/SKILL.md` includes a pattern for detecting and blocking commits on already-merged `squad/*` branches.

#### Evidence Review

**What Has Happened (Sprint 1–2)**

1. **No reported merged-branch incidents** in MyBlog's recent history (Sprints 0–2, PRs #6–#15)
   - 10 PRs successfully merged with proper cleanup
   - No orphaned commits or stranded history observed
   - Post-merge cleanup already documented in `.squad/playbooks/pr-merge-process.md` (Step 8)

2. **Existing safeguards already in place**
   - `.squad/playbooks/pr-merge-process.md` includes explicit Post-Merge Orphan Branch Cleanup ceremony (Ralph's responsibility)
   - `docs/CONTRIBUTING.md` includes merged-branch awareness section and recovery steps (added Sprint 1.2)
   - `.squad/skills/merged-pr-guard/SKILL.md` is already routed into `.squad/routing.md` as guidance for "Resumed work on existing squad/* branch"

3. **Small team size reduces pressure**
   - MyBlog is a single-author/small-team project (Aragorn as lead, 6 domain agents)
   - Contributor workflow is highly visible and self-correcting
   - Manual review gates (PR process, Aragorn's leadership) catch branch issues before commit

4. **Automation would add friction without demonstrated ROI**
   - Pre-commit guard requires `git pre-commit` hook (separate from pre-push)
   - Extra validation logic before every commit (slow local workflow)
   - No real incidents to justify the cost
   - Contributing guidance already covers the anti-pattern

#### Decision

**Keep merged-branch guidance in routing and docs; defer/do-not-implement the automation.**

##### Rationale

1. **The guidance is sufficient for MyBlog's current scale**
   - Documented recovery path in `CONTRIBUTING.md` (contributors know what to do if they encounter a merged branch)
   - Routed into squad workflow via `.squad/routing.md` (agents are reminded when resuming work)
   - Post-merge cleanup is already part of the formal PR merge ceremony

2. **No operational incidents justify the added complexity**
   - 15 consecutive PRs with clean merges and cleanup
   - No orphaned history or stranded commits observed in any session
   - Manual awareness is working

3. **Lighter is correct for this repo's risk profile**
   - Single-author focus; contributors are invested squad members, not anonymous public contributors
   - Sprint 1.2 merged-branch awareness guidance is sufficient to catch issues at code-review time
   - If merged-branch issues become frequent, automate then with real data

4. **The skill stays available if needed later**
   - `.squad/skills/merged-pr-guard/SKILL.md` remains in the repo
   - If future sessions report stranded commits, the skill can be referenced
   - Milestone 3 (Adapt-or-Delete Cleanup) can revisit this when/if frequency warrants it

#### Action

**No code changes required.** The decision is to preserve the existing guidance-only approach:

- ✅ `.squad/skills/merged-pr-guard/SKILL.md` — remains as reference material, not automated
- ✅ `.squad/playbooks/pr-merge-process.md` Step 8 — remains as formal cleanup ceremony
- ✅ `docs/CONTRIBUTING.md` "After Your PR Is Merged" section — remains as contributor guidance
- ✅ `.squad/routing.md` — continues to route the skill for awareness when resuming work

**Do not implement:**

- ❌ Pre-commit hook guard
- ❌ Workflow automation to detect merged branches
- ❌ Additional enforcement logic

#### Transition

This decision resolves Milestone 2 Sprint 2 backlog item #11. No follow-up work needed unless:

1. **Future sessions report merged-branch incidents** (stranded commits, orphaned history) — then escalate to automation
2. **Team grows significantly** and manual awareness breaks down — then revisit with evidence
3. **Milestone 3 adapt-or-delete pass** finds the skill unused — then archive or delete intentionally

For now: **Closed as "defer automation, keep guidance."**

#### Related Assets

- **Skill:** `.squad/skills/merged-pr-guard/SKILL.md`
- **Playbook:** `.squad/playbooks/pr-merge-process.md` (Step 8: Post-Merge Cleanup)
- **Contributing Guide:** `docs/CONTRIBUTING.md` ("After Your PR Is Merged" section)
- **Routing:** `.squad/routing.md` (merged-pr-guard routed for resumed work)

---

### 13. Release Guidance Fit for MyBlog

**Date:** 2026-04-19  
**Author:** Aragorn (Lead Developer)  
**Status:** ✅ Final Decision

#### Context

The imported release assets still referenced IssueTrackerApp, upstream release workflows, and generic automation patterns that do not exist in MyBlog. The live repo only has `dev`/`main` branch governance, `GitVersion.yml`, `ci.yml`, and a hotfix backport reminder workflow.

#### Decision

1. Active release guidance for squad work is now MyBlog-specific:
   `.squad/skills/release-process/SKILL.md` routes release work to
   `.squad/playbooks/release-myblog.md`.
2. The old IssueTrackerApp playbook is replaced by
   `.squad/playbooks/release-myblog.md`.
3. `.squad/skills/release-process-base/SKILL.md` is quarantined and must not be
   injected into normal MyBlog work.
4. Normal `dev` → `main` releases do **not** require syncing `main` back into
   `dev` after merge. Only hotfixes merged to `main` require a backport to `dev`.

#### Consequences

- Release guidance now matches the repo's actual branch model and workflows
- The team has a clear owner path: Aragorn approves release scope; Boromir runs
  the operational steps
- Generic release automation language is explicitly out of scope until MyBlog
  actually adds those workflows
- Sprint 3 can safely delete the quarantined generic base skill unless a new
  template-use case is approved

---

### 14. Delete Remaining Non-Fit Imported Squad Assets

**Date:** 2026-04-19  
**Author:** Aragorn (Lead Developer)  
**Status:** ✅ Final Decision

#### Context

Milestone 2 already settled two direct deletions: `post-build-validation` and `static-config-pattern`. Other imports were kept only as quarantine context while the team finished repo-fit work: `building-protection` and `release-process-base`.

Sprint 3 now has the needed follow-through context:

1. MyBlog-specific release guidance exists in
   `.squad/skills/release-process/SKILL.md` and
   `.squad/playbooks/release-myblog.md`.
2. No decision ever approved a live MyBlog use case for the Minecraft-only
   `building-protection` skill.
3. The old `release-MyBlog` playbook has already been replaced and should
   remain deleted.

#### Decision

1. Execute the already-approved deletions for
   `.squad/skills/post-build-validation/` and
   `.squad/skills/static-config-pattern/`.
2. Delete `.squad/skills/building-protection/` because its quarantine was
   temporary and no explicit keep decision exists.
3. Delete `.squad/skills/release-process-base/` because the MyBlog-specific
   release workflow replaced the generic template and no template-retention
   decision was approved.
4. Keep `.squad/playbooks/release-myblog.md`,
   `.squad/skills/release-process/SKILL.md`, and
   `.squad/skills/microsoft-code-reference/SKILL.md` unchanged.
5. Treat `.squad/decisions/DELETED-ASSETS.md` as the published manifest for the
   final disposition state.

#### Consequences

- Normal squad routing now only references assets with an active MyBlog fit.
- The remaining imported catalog is smaller and less likely to mislead future
  contributors with quarantined-but-dead guidance.
- Any future reintroduction of these deleted assets now requires a new explicit
  architecture decision instead of silent reuse.

#### Related Asset Manifest

See `.squad/decisions/DELETED-ASSETS.md` for comprehensive documentation of all deletions, including building-protection, release-process-base, post-build-validation, static-config-pattern, and release-MyBlog.

---

## Decision 15: PR Check Monitoring and Async Tool Handling

**Date:** 2026-04-19  
**Author:** Boromir (DevOps)  
**Scope:** CI/CD, PR merge process  
**Status:** Observation & Best Practice

### Summary

When monitoring PR checks during the merge gate process, the "Agent" check (GitHub Copilot code review) and other async background jobs run independently and do not block PR mergability or review readiness. Only explicitly required checks block merge.

### Context

During PR #16 creation and check monitoring:

- 4 core test suites (Architecture, Unit, Integration, Coverage) completed successfully within ~60 seconds
- 2 additional checks remain in-progress: Agent (Copilot review) and build-and-test (secondary CI job)
- PR status: OPEN, MERGEABLE, ready for human review

### Decision

**Do not wait for optional async checks before declaring a PR "ready for review".**

Required checks:

- Test Results ✅
- Architecture Tests ✅
- Unit Tests ✅
- Integration Tests ✅
- Coverage Summary ✅
- Prepare ✅

Optional/informational (do not block):

- Agent (async Copilot review)
- build-and-test (duplicate secondary job; tests already covered by main suite)

### Implication

Squad members can proceed to review and merge once all required checks pass, even if async background jobs are still running.

### Related Files

- `.github/workflows/ci.yml` (defines required vs. optional checks)
- `.github/workflows/squad-test.yml` (parallel test matrix)
- `.squad/playbooks/pr-merge-process.md` (merge gate playbook)

---

## Decision 16: Recurring "Merging is Blocked" Root Cause & Mitigation

**Date:** 2026-04-19  
**Decision Owner:** Boromir (DevOps)  
**Impact:** Repository maintainability, PR merge velocity  
**Status:** ✅ Investigation Complete — **Recommend Option 1**

### Problem Statement

Team repeatedly encounters GitHub's "Merging is blocked" error during PR merges, despite all required CI checks passing and reviewers approving. This blocks merge progress and requires escalation to repo owner.

**Observed in:**

- PR #13 (first block)
- PR #14 (same block, documented)
- PR #15, #16, #17 (blocked but resolved via manual thread closure)

### Root Cause

**Ruleset Enforcement Discovery:**

Repository is protected by GitHub Ruleset `protectbranch` (ID: 15246849) with the following configuration:

```json
{
  "enforcement": "active",
  "conditions": {
    "ref_name": {
      "include": ["refs/heads/main", "refs/heads/dev"]
    }
  },
  "rules": [
    {
      "type": "pull_request",
      "parameters": {
        "required_review_thread_resolution": true
      }
    }
  ],
  "bypass_actors": []
}
```

**Why This Blocks Every PR:**

1. Copilot-pull-request-reviewer[bot] creates ~8–10 review threads per PR (standard code review)
2. Rule `required_review_thread_resolution: true` requires ALL threads to be marked "Resolved"
3. Replying in a thread does NOT count as resolved — only clicking GitHub UI "Resolve" button marks it resolved
4. Only a human reviewer can explicitly resolve (bot cannot resolve its own threads)
5. Merge fails until all threads are manually resolved
6. `bypass_actors: []` is empty, so even repo owner cannot use `--admin` to force merge

### Decision: Add Admin Bypass Actor

#### Recommendation

**Implement Option 1: Add RepositoryOwner to bypass_actors**

Modify ruleset `protectbranch` pull_request rule bypass actors:

```json
bypass_actors: [
  {
    "type": "Actor",
    "actor_type": "RepositoryOwner"
  }
]
```

#### Rationale

| Aspect                | Justification                                                                                   |
| --------------------- | ----------------------------------------------------------------------------------------------- |
| **Effectiveness**     | ✅ Allows repo owner to override merge block if needed; keeps rule enforced for all team members |
| **Risk**              | ✅ Low — only repo owner gains override; team workflows unchanged                                |
| **Permanence**        | ✅ Persistent configuration change; one-time setup                                               |
| **Effort**            | ✅ Minimal — single API call or UI toggle                                                        |
| **Alternative Risks** | ❌ Disabling thread resolution entirely weakens review rigor; audit mode defeats protection      |
| Aspect | Justification |
| -------- | --------------- |
| **Effectiveness** | ✅ Allows repo owner to override merge block if needed; keeps rule enforced for all team members |
| **Risk** | ✅ Low — only repo owner gains override; team workflows unchanged |
| **Permanence** | ✅ Persistent configuration change; one-time setup |
| **Effort** | ✅ Minimal — single API call or UI toggle |
| **Alternative Risks** | ❌ Disabling thread resolution entirely weakens review rigor; audit mode defeats protection |

#### Implementation Steps

1. Navigate to: GitHub Repo → Settings → Rules → `protectbranch`
2. Click "Edit" on the pull_request rule
3. Under "Bypass actors," add: Repository Owner
4. Publish and save

#### Verification

```bash
gh api repos/mpaulosky/MyBlog/rulesets/15246849 --jq '.bypass_actors'
# Expected output: [{"type":"Actor","actor_type":"RepositoryOwner",...}]
```

### Alternatives Considered

#### Option 2: Disable Thread Resolution Requirement

- **Setting:** `required_review_thread_resolution: false`
- **Impact:** Threads still posted but no longer block merge
- **Risk:** ⚠️ Medium — threads might be routinely ignored by team
- **Recommended:** ❌ No — weakens code review discipline

#### Option 3: Set Ruleset to Audit Mode

- **Setting:** `enforcement: "audit"`
- **Impact:** Rules no longer block; only logged for monitoring
- **Risk:** ⚠️ High — defeats entire protection mechanism
- **Recommended:** ❌ No — not a permanent fix, defeats purpose

#### Option 4: Manual Thread Resolution (Current Workaround)

- **Process:** Team manually clicks "Resolve" for each Copilot thread before merge
- **Impact:** Merge unblocked but repetitive and error-prone
- **Risk:** ⚠️ Medium — forgotten threads re-block merge; high friction
- **Recommended:** ❌ No — status quo, not scalable

### Decision Outcome

**✅ IMPLEMENT OPTION 1 immediately**

- **Action:** Add RepositoryOwner to bypass_actors for `protectbranch` ruleset
- **Owner:** Repository owner (mpaulosky)
- **Timeline:** <5 minutes setup
- **Verification:** Run `gh api` command above to confirm bypass_actors populated
- **Impact:** Future PRs will merge cleanly once all required checks pass and reviewers approve, even if Copilot threads exist (owner can force merge if needed; team still resolves threads per norm)

### Learnings for Future DevOps Setup

1. **Always configure bypass_actors** when setting up branch protection rulesets
2. **Rulesets are stricter than branch protection rules** — they take precedence and ignore CLI flags like `--admin` unless bypass actors are configured
3. **Thread resolution is semantic** — GitHub API distinguishes "replied" from "resolved"; only explicit resolution counts
4. **Copilot bot reviews generate high volume** — 8–10 threads per PR is standard; high friction if manual resolution required

---

## Decision 17: PR #19 CI Blockage Root Cause & Remediation

**Date:** 2026-04-19T15:10:00Z  
**Owner:** Boromir (DevOps)  
**Status:** ✅ COMPLETE — PR merged to dev  
**Affects:** PR #19 (MERGED), `build-and-test` required status check (unblocked)

### Problem Statement

PR #19 ("chore: remove orphan root diff artifact from branch") was blocked at merge with:

```text
Repository rule violations — Required status check 'build-and-test' is expected
```

The PR itself is safe and approved by both Aragorn (lead) and Boromir (DevOps). The blockage is due to CI environment, not code.

### Root Cause Analysis

1. **Repository Ruleset Configuration:**
   - Ruleset: `protectbranch` (ID 15246849, active, enforces on `refs/heads/main` and `refs/heads/dev`)
   - Required rule: `required_status_checks` with context `build-and-test`
   - Policy: strict (cannot merge if check missing or in non-terminal state)

2. **CI Workflow Status:**
   - Run ID: 24631882902 (latest on `copilot/clean-orphan-changes`)
   - Status: `completed`
   - Conclusion: **`action_required`** (not `success`, `failure`, or `skipped`)
   - Root cause: Firewall block on compass.mongodb.com (MongoDB connectivity)
   - Evidence: PR body contains Copilot firewall warning confirming DNS block

3. **Why This Happened:**
   - CI workflow ran during Copilot agent session with firewall rules enabled
   - Integration tests or test setup attempted MongoDB connection
   - Firewall rules (enabled during Copilot agent runs) blocked access to compass.mongodb.com
   - Result: workflow halted in `action_required` state (requiring manual intervention)

### Mechanical Remedy Applied & Result

**Action:** Triggered workflow rerun  
**Command:** `gh run rerun 24631882902`  
**Result:** ✅ SUCCEEDED

- New run started: 2026-04-19T15:05:10Z
- New run completed: 2026-04-19T15:06:36Z  
- Conclusion: `success`
- All required status checks now passing

**Verification:**

```bash
$ gh pr view 19 --json mergeStateStatus,mergeable
{
  "mergeStateStatus": "CLEAN",
  "mergeable": "MERGEABLE"
}
```

### Next Steps & Final Status

#### ✅ COMPLETE — PR #19 MERGED

**Merge Execution:**

```bash
$ gh pr merge 19 --squash --delete-branch
✓ Squashed and merged pull request mpaulosky/MyBlog#19 (chore: remove orphan root diff artifact from branch)
✓ Deleted remote branch copilot/clean-orphan-changes
```

**Merge Details:**

- Merged at: 2026-04-19T15:07:38Z by mpaulosky
- Target branch: `dev`
- Commit SHA: 04ba254
- Files changed: 1 (pr2-diff.txt deleted, 1698 lines removed)
- Remote branch: deleted

**Final PR Status:**

- `state: MERGED` ✅
- Branch cleanup: complete
- Remote tracking: removed

### Outcome Summary

**PR #19 is fully closed and merged to dev.** The artifact cleanup has been integrated into the development branch per playbook standards. No further action required.

### Context for Future Reference

**Why `action_required` Occurs:**

- GitHub Actions enters `action_required` state when:
  1. A workflow step requires approval/manual intervention
  2. Firewall/environment blocks execution mid-workflow
  3. Required context setup fails (rare)
- This status blocks merge even though the workflow code itself is sound

**Rerun Strategy:**

- Rerunning is appropriate when `action_required` is due to environment, not code
- Evidence: PR body contained firewall warning; tests pass on code inspection; no recent code changes

**Merge Verification:**

- Before merge: `mergeStateStatus: CLEAN`, `mergeable: MERGEABLE`
- All required checks: passing
- Review decision: none required for artifact cleanup PRs (non-code change)

### Decision (Final)

**Boromir Decision:** PR #19 investigation complete. Workflow rerun succeeded, all checks passed, PR merged to dev. Artifact cleanup integrated.

**Status:** ✅ COMPLETE

### 15. Squad Maintenance Review — Issue #222

**Status:** ✅ Reviewed & Integrated  
**Date:** 2026-05-05  
**Reviewed by:** Aragorn (Lead Developer)  
**Branch:** `squad/222-squad-maintenance`

Complete squad infrastructure maintenance pass covering all 7 `.squad/` files: team roster, routing rules, agent charters, decision formatting, identity state, and skill catalog.

**Fixes Applied:**

1. **`identity/now.md` YAML timestamp** — Fixed malformed UTC timestamp from `2026-04-19T03:35:` (truncated) to `2026-04-19T03:35:00Z` (valid ISO 8601)
2. **`decisions.md` duplicate heading** — Removed erroneous `### 6.` heading that duplicated the section title; section numbering corrected

**Validated as Correct:**

- `team.md` — table alignment improved; member roster and capability matrix accurate
- `routing.md` — routing guardrails and skills injection rules intact after formatting cleanup
- Agent charters (Aragorn, Bilbo) — structure and content validated; Bilbo charter expanded with blog-specific guidance
- `decisions.md` — decision history, PR references, and metadata all preserved
- `.squad/skills/` catalog — all 20+ skill definitions intact

**Identified for Follow-Up:**

1. `identity/now.md` field structure — YAML front-matter fields (`focus_area`, `active_issues`) duplicate Markdown body content; recommend consolidation to YAML-only for single source of truth
2. `decisions.md` numbering — Pre-existing issue: decisions 2a and 2b both use `### 2.`; requires renumbering (2 → 3 → ... 14) in a dedicated pass
3. Member charter audit — Several members lack fully populated charters; recommend comprehensive audit for Identity, Expertise, Responsibilities, Boundaries, Critical Rules, Model sections

**PR Status:** Merged to `dev` with Closes #222

### 16. ThemeProvider Placement in Routes.razor (Not App.razor)

**Status:** ✅ Decided  
**Date:** 2026-05-07  
**Decided by:** Legolas (Frontend / Blazor)  
**Issue:** #238 — Fix light/dark theme toggle  
**Branch:** `squad/238-fix-light-dark-theme-toggle`

**Context:**

The light/dark theme toggle was inert in the live app. The root cause was `ThemeProvider` being placed in or near `App.razor`, which runs in a static pre-render boundary. `ThemeSelector` in `NavMenu` consumed the cascade but the interactive render boundary isolated it from the actual ThemeProvider instance.

**Decision:**

**`ThemeProvider` MUST live in `Routes.razor`, wrapping `<Router>` as its outermost child. It MUST NOT be placed in `App.razor` or outside the interactive subtree.**

```razor
<!-- Routes.razor — CORRECT -->
<ThemeProvider>
    <Router AppAssembly="typeof(Program).Assembly" ...>
        ...
    </Router>
</ThemeProvider>
```

**Rationale:**

- `Routes.razor` shares the interactive render boundary with `MainLayout`, `NavMenu`, and `ThemeSelector` — the cascade flows without crossing any render-mode fence.
- `App.razor` is a static pre-render host component. Blazor cascades do not reliably cross the static-to-interactive boundary.
- `NavMenu.razor` must NOT declare `@rendermode InteractiveServer` — that creates a nested boundary that would again isolate the toggle.

**Enforcement:**

`tests/Architecture.Tests/ThemeRenderBoundaryTests.cs` contains three tests that enforce this structure as a compile-time regression guard:

1. `RoutesShouldWrapRouterInsideThemeProvider` — Routes.razor contains `<ThemeProvider>`
2. `AppRazorShouldNotContainThemeProvider` — App.razor does NOT contain `<ThemeProvider>`
3. `NavMenuShouldNotDeclareRenderMode` — NavMenu.razor has no `@rendermode`

**Related:**

- Anti-FOUC IIFE in `App.razor` `<head>` is separate from the interactive provider and reads `theme-color` / `theme-mode` split keys from localStorage synchronously
- `themeManager.markInitialized()` called from `ThemeProvider.OnAfterRenderAsync` sets `data-theme-ready="true"` on `<html>` for E2E test readiness detection

### 17. AppHost Theme Runtime Tests Must Enable Static Web Assets in Testing Environment

**Status:** ✅ Decided  
**Date:** 2026-05-07  
**Decided by:** Boromir (DevOps / Infra)  
**Issue:** #238  
**Branch:** `squad/238-fix-light-dark-theme-toggle`

**Context:**

The AppHost Playwright harness launches the web app under
`ASPNETCORE_ENVIRONMENT=Testing`. In that environment, the browser test never
became a trustworthy interactive Blazor page until static web assets were
explicitly enabled. Before the fix, the runtime diagnostics showed that the
theme bootstrap script loaded, but the page still failed to hydrate correctly
because the AppHost test host could not serve several required assets:

- `/_framework/blazor.web.js`
- `/Components/Layout/ReconnectModal.razor.js`
- `/Web.styles.css`

A separate asset mismatch was also present in `App.razor`: the app referenced `MyBlog.Web.styles.css`, but the correct scoped CSS bundle name is `Web.styles.css`.

**Decision:**

- In `src/Web/Program.cs`, call `builder.WebHost.UseStaticWebAssets()` when the environment is `Testing`.
- In `src/Web/Components/App.razor`, reference the scoped CSS bundle as `@Assets["Web.styles.css"]`.
- Keep the AppHost runtime theme test focused on the real user flow:
  - open `/`
  - toggle light → dark
  - navigate to `/blog`
  - verify that the dark theme persists there
- Assert the `/blog` page with a stable accessible heading selector instead of a generic first-`h1` lookup.

**Rationale:**

- The failure was not an Aspire proxy problem and not just a timing problem. The page stayed prerender-only because the Blazor runtime and related static assets were unavailable in the `Testing` host configuration.
- Enabling static web assets in `Testing` is a small, harness-enabling change that preserves normal published behavior while making the AppHost browser tests representative.
- Correcting the scoped CSS asset name removes a real app-shell asset bug that would otherwise remain masked.
- Once the harness hydrated correctly, the remaining failure was a normal test assertion issue, which confirmed the root infrastructure problem had been resolved.

**Validation:**

- Focused runtime persistence test passed: `ThemeToggle_DarkMode_PersistsAfterNavigatingToBlogPosts`
- Focused AppHost theme slice passed: Passed 2, Skipped 1, Failed 0
- Focused architecture theme slice passed: Passed 5, Failed 0
- Focused bUnit theme slice passed: Passed 33, Failed 0

**Follow-up:**

- No immediate Gimli follow-up is required for the `/blog` persistence path.
- The only remaining AppHost theme gap is the separate, already-documented seeded-localStorage reload/bootstrap race in `LayoutThemeToggleTests`.

### 18. Theme Toggle Runtime Coverage Uses Dynamic Skip Until AppHost Testing Catches Up

**Status:** ✅ Decided  
**Date:** 2026-05-07  
**Decided by:** Gimli (Tester)  
**Issue:** #238  
**Branch:** `squad/238-fix-light-dark-theme-toggle`

**Context:**

Manual browser verification on the live app confirmed the issue #238 fix:
clicking the theme toggle updates `<html>.dark`, `localStorage['theme-mode']`,
and the toggle aria-label.

The AppHost Playwright harness does not observe the same runtime state under
`ASPNETCORE_ENVIRONMENT=Testing`. In focused runs, the header toggle stayed at
`Toggle dark mode (currently light)` and the new runtime readiness marker
`data-theme-ready` never appeared. The latest AppHost test update now attempts
the exact user flow that Boromir requested: open `/`, wait for theme readiness,
toggle light → dark, navigate via the `Blog Posts` link to `/blog`, verify theme
persistence on the Blog Posts page.

In the current harness, that flow still blocks before the test can trust the toggle as interactive.

**Decision:**

- Keep `LayoutThemeToggleTests.cs` statically skipped for the seeded-storage reload/bootstrap race.
- Convert `ThemeToggleInteractionTests.cs` into an xUnit v3 dynamic-skip runtime probe that tries the real light/dark → `/blog` persistence flow first and only skips when the AppHost Testing harness never becomes trustworthy.
- Add architecture regression tests that lock down the fix's structural invariants:
  - `ThemeProvider` wraps the router in `Routes.razor`
  - `App.razor` keeps only `<Routes @rendermode="InteractiveServer" />`
  - `NavMenu.razor` contains no nested `@rendermode`
- Keep relying on the focused `Web.Tests.Bunit` theme tests for component-level behavior until the AppHost Testing environment matches the live app closely enough for meaningful runtime automation.

**Rationale:**

- The live bug was caused by render-boundary placement, so source-structure tests directly guard the highest-risk regression points.
- A failing or misleading AppHost runtime test in the Testing environment would create noise instead of confidence.
- Dynamic skip is the honest middle ground: the runtime test now exercises the real user path whenever the harness improves, but still reports the exact blocker instead of failing for the wrong reason or pretending to pass.
- Structural guards and bUnit coverage remain the strongest trustworthy safety net while the AppHost Testing harness still misses interactive theme hydration.

**Validation:**

- `dotnet test tests/AppHost.Tests --filter Theme`: Passed 1, Failed 0, Skipped 2
- `dotnet test tests/Architecture.Tests --filter Theme`: Passed 5, Failed 0, Skipped 0
- `dotnet test tests/Web.Tests.Bunit --filter Theme`: Passed 37, Failed 0, Skipped 0
- `dotnet test tests/Architecture.Tests`: Passed 15, Failed 0, Skipped 0

**Related Pattern: bUnit Cascade Integration Tests**

Full-pipeline cascade tests were added to `ThemeSelectorTests.cs` and a
readiness-marker test to `ThemeProviderTests.cs`. These prove the
`ThemeBrightnessToggleComponent → ThemeSelector → ThemeProvider` chain in bUnit
without requiring E2E infrastructure. Pattern: render Provider with Selector as
child content, trigger child event, assert Provider's cascaded state updates.

### 19. Pre-Commit Markdownlint Gate

**Date:** 2026-04-25
**Author:** Aragorn (Lead / Architect)
**PR:** #232
**Branch:** `squad/230-precommit-markdownlint-gate`

## Context

PR #229 fixed 3,243+ markdownlint violations across all `.md` files and added `.markdownlint.json` to the repo root. Without a commit-time gate, those violations could easily regress as contributors edit documentation.

## Decision

Add a pre-commit git hook (`.github/hooks/pre-commit`) that runs `markdownlint-cli2` on staged `.md` files only before each local commit.

## Rationale

- **Staged-only linting** is fast — no full-repo scan on every commit.
- **Graceful degradation** — warns but does not block if the binary is absent, preserving developer ergonomics for contributors who have not run `npm install`.
- **Consistent config** — reuses the existing `.markdownlint.json` so rules cannot diverge between the hook and CI.
- **Pattern consistency** — mirrors the pre-push hook pattern already established in `.github/hooks/pre-push` and `scripts/install-hooks.sh`.

## Implementation

- `.github/hooks/pre-commit` — the hook source (tracked in git)
- `scripts/install-hooks.sh` — updated to install both `pre-push` and `pre-commit` hooks
- `package.json` — added `markdownlint-cli2 ^0.17.2` as dev dependency

## Binary probe order

1. `markdownlint` (global CLI)
2. `./node_modules/.bin/markdownlint-cli2`
3. `./node_modules/.bin/markdownlint`
4. Not found → warn, exit 0 (graceful degrade)

## Trade-offs

- `package-lock.json` grows with the new dependency — acceptable for a DX tooling dep.
- Contributors must run `npm install` to get the linter; the hook warns them if they haven't.
