# Pippin — History

## Learnings

### MyBlog Project Context (2025)

**Files Updated/Created:**
- `/README.md` — Complete rewrite: Changed from NewProject (issue tracker) to MyBlog (training blog)
- `/docs/CONTRIBUTING.md` — Updated all placeholders with MyBlog-specific URLs, tech stack, design principles
- `/docs/ARCHITECTURE.md` — Created new: 800+ lines covering solution structure, layers, entity design, testing strategy
- `/.squad/agents/pippin/charter.md` — Fixed: Changed role from "Tester (E2E)" to "Docs"

**Key Facts About MyBlog:**
- Training/learning project, not production. Stack: .NET 10, Aspire 13.2.2, Blazor Server (ISR)
- No database/auth/cache by design — InMemoryBlogPostRepository only
- 9 tests total: 7 unit tests (BlogPost, repository), 2 architecture tests (NetArchTest.Rules)
- Domain model: `BlogPost` entity with factory method `Create()`, mutable methods `Update()`, `Publish()`, `Unpublish()`
- Clean architecture: Web → Domain dependency, Domain has no external deps
- Short project names (AppHost, Domain, Web, ServiceDefaults) — no MyBlog. prefix; repo name provides context
- Tests all passing; uses xUnit, FluentAssertions, NSubstitute
- Blazor Server components: Index, Create, Edit pages + ConfirmDeleteDialog

**Documentation Principle:**
- Keep docs accurate and scoped to what project actually is
- Never add features/tech that don't exist in codebase
- Links point to real repo (mpaulosky/MyBlog), not template repos

## 2026-04-19 — CONTRIBUTING.md Pre-Push & PR Sections (Skills Review)

As part of DevOps skills/playbooks review, Pippin assigned to update CONTRIBUTING.md with pre-push validation gates and PR review process.

**Action:** Add two new sections to CONTRIBUTING.md (1h) — see Frodo's history for details.

**Collaboration:** Pippin + Frodo (CONTRIBUTING.md co-owners).

**Timeline:** Week 1 (1h estimated).
## 2026-04-19 — Sprint 1.2 Completion — Governance & Documentation Alignment

Completed Milestone 1b (Sprint 1.2) by aligning governance and documentation to reflect Sprint 1.1 hardened workflow.

**Two Major Decisions Recorded:**

### Decision 10: Document Guardrails Update

Updated `docs/CONTRIBUTING.md` to accurately reflect post-Sprint 1.1 enforced workflow:
- Branch naming enforcement: `squad/{issue}-{slug}` (not optional)
- Automatic hook installation on clone via post-checkout
- 5 sequential pre-push gates (clear descriptions, retry behavior)
- Troubleshooting section with concrete examples
- PR workflow alignment (CI wait gate, squash-merge flow)

**Impact:** Contributors now see the actual workflow, reducing onboarding friction and failed pushes.

### Decision 11: Merged-Branch Awareness Guidance

Added lightweight guidance section to CONTRIBUTING.md warning about committed on merged branches:
- Voluntary (not enforced yet)
- Safe recovery path: `git checkout main && git pull && squad/{new-issue}-{slug}`
- Defers automation (pre-commit guard) to Sprint 2 frequency review

**Impact:** Contributors have clear path if they accidentally work on merged branch; team can measure frequency before automating.

**Files Modified:**
- `docs/CONTRIBUTING.md` — +3 new sections (branch naming, auto-install, merged-branch awareness)

**Timeline:** Completed as part of coordinated M1.2 effort with Aragorn.

**Outcome:** ✅ CONTRIBUTING.md now matches Sprint 1.1 enforced reality.

## 2026-04-19: Milestone 3 Roadmap Completion (Final)

**Milestone:** 3 (Adapt-or-Delete Cleanup & Roadmap Completion)  
**Outcome:** ✅ Complete

Published comprehensive deleted-assets manifest and coordinated final roadmap consolidation for Milestone 3.

### Key Achievements

1. **Deleted-Assets Manifest Published**
   - Created `.squad/decisions/DELETED-ASSETS.md` as canonical record
   - Documented 4 deleted skills: post-build-validation, static-config-pattern, building-protection, release-process-base
   - Documented 1 deleted playbook: release-issuetracker
   - Documented 1 retained-&-clarified asset: microsoft-code-reference
   - Provided clear rationale & decision cross-references for each entry
   - Published reference table for future contributor triage

2. **Asset Disposition Summary Prepared**
   - Formatted decision table for integration into decisions.md
   - Verified alignment with Aragorn (release guidance fit) and Boromir (secondary skills assessment)
   - Created actionable manifest structure for future deletions

### Cross-Team Coordination

- **Coordinated with Aragorn:** Release guidance & delete decisions (Decision #13, #14) — provided asset context & removal sequence
- **Coordinated with Boromir:** Merged-branch guard evidence review — contributed frequency baseline
- **Routed with Scribe:** Decision inbox merged; manifest published; agent history cross-linked

### Modified Assets

- Manifest published: `.squad/decisions/DELETED-ASSETS.md` (authoritative reference)
- Decision merged: Decision #14 (Delete non-fit assets) → `.squad/decisions.md` (with manifest cross-ref)
- Orchestration logged: `2026-04-19T04-04-30-pippin-sprint-3-manifest.md`

### Roadmap Impact

- Milestone 3 "Adapt-or-Delete" pass now complete with published manifest
- Contributors have single authoritative source for "why was X removed?"
- Manifest provides template for future deletions (consistent structure)
- Supports lean catalog commitment: remove non-fit instead of archive
- Follows Milestone 2 skill mining finalization

**Constraints Satisfied:**
- ✅ Manifest format matches squad decision conventions  
- ✅ Asset disposition table provides at-a-glance triage  
- ✅ No contradictory reasoning across decisions  
- ✅ Future-proof structure for additional deletions  
