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
   - Documented 1 deleted playbook: release-MyBlog
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

---

## Sprint 7: xUnit v3 Migration ADR

### Work Summary

**Issue #166: Document xUnit v3 migration decision + performance analysis**

Authored comprehensive Architecture Decision Record (ADR) for xUnit v3 migration pilot, establishing rationale, rollout strategy, and performance metrics for phased adoption across test projects.

### Key Decisions Documented

1. **Migration Strategy**: Incremental pilot starting with Domain.Tests (Sprint 7), followed by selective rollout to other projects in Sprints 8–13
2. **Performance Baseline**: Measured Domain.Tests at 104 ms (42 tests), with projected 5–15% improvement under xUnit v3 (~88–99 ms)
3. **API Changes**: Documented breaking changes (`[Fact]` → `[Test]`, `TheoryData<T>` adjustments) with before/after examples
4. **Per-Project Versioning**: Established Directory.Packages.props strategy to allow coexistence during migration
5. **Risk Mitigation**: Fallback plan if pilot discovers critical blockers; measurement strategy for validating ecosystem benchmarks

### ADR Structure & Content

- **File**: `docs/adr/sprint7-xunit-v3-migration.md`
- **Front Matter**: YAML metadata (post_title, author, categories, tags, ai_note, summary, post_date)
- **Sections**:
  - Context: Why xUnit v3? (MTP adoption, performance, ecosystem alignment)
  - Decision: Adopt v3 incrementally, pilot scope (Domain.Tests)
  - Rationale: Risk containment, learning opportunity, data-driven decisions
  - Consequences: Positive (ecosystem alignment, performance, tooling) + Negative (API breaking changes, migration effort, IDE lag)
  - Performance Analysis: Baseline metrics, projected improvements, measurement plan
  - Rollout Plan: Sprints 7–13 timeline with decision gates
  - Alternatives: All-at-once migration (rejected—high risk); stay on v2 indefinitely (rejected—technical debt); wait for tool support (rejected—MTP is new standard)
  - Appendix: Test rewrite example (v2 → v3 code comparison)

### Validation

- ✅ Release build passes (dotnet build Release)
- ✅ ADR format matches existing conventions (sprint5-caching-abstraction.md)
- ✅ Performance data sourced from baseline test run (Domain.Tests: 104 ms)
- ✅ Markdown validated against .github/instructions/markdown.instructions.md
- ✅ No .squad/ governance files modified (Protected Branch Guard)
- ✅ PR #169 created, targeting sprint/7-xunit-v3-pilot branch

### Learnings

1. **ADR Format Maturity**: MyBlog's ADR structure is well-established; YAML front matter + markdown sections work well for technical decisions
2. **Performance Metrics Matter**: Including concrete baseline (104 ms) + projected range (5–15%) makes decision more credible to reviewers
3. **Incremental Migration is Standard**: xUnit v3 ecosystem expects phased adoption; pilot-first approach aligns with community patterns
4. **MTP as Future Standard**: Microsoft Testing Platform is now the default for .NET testing; early adoption positions project ahead of curve
5. **API Compatibility Risk**: Breaking changes (`[Fact]` → `[Test]`) require careful documentation to set expectations for developers

### Related Issues & PRs

- **Issue #163**: Domain.Tests package swap (xunit → xunit.v3, OutputType=Exe)
- **Issue #164**: Domain.Tests API rewrite (adjust test method signatures)
- **PR #169**: xUnit v3 migration ADR (targeting sprint/7-xunit-v3-pilot)
- **Sprint 7 Milestone**: xUnit v3 pilot validation

### Team Coordination

- **Collaborators**: Gimli (tester, performance validation), Boromir (sprint coordination)
- **Aragorn Review**: Architecture lead reviews decision rationale & alternatives
- **Scribe Note**: Decision will be merged into shared decisions.md after squad review

### Next Steps (by others)

- Gimli: Implement issues #163–#164 (package swap + API rewrite in Domain.Tests)
- Measure performance post-migration (compare 104 ms v2 baseline to v3 actual)
- Plan Sprint 8 migration target (likely Architecture.Tests, small project)
- Validate CI/CD + code coverage tooling compatibility with MTP

---
