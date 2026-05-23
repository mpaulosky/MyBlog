# Decision #32: PR #295 Squash Merge & Sprint 19 Triage Pattern

**Date:** 2026-05-15  
**Author:** Aragorn  
**Stakeholders:** Boromir (work-check), Legolas (UI), Gimli (testing)  
**Status:** Ready for Scribe merge

## Context

PR #295 (`squad/291-input-css-fine-tuning` → `dev`) completed dark-mode colour fixes, PageHeadingComponent introduction, and button variant consolidation. All 19 CI checks were green; Copilot automated review commented (no changes requested); all 6 inline threads resolved.

Simultaneously, issue #294 (exact duplicate of #293) and two other Sprint 19 issues (#293, #296) required triage, sprint-stamping, and label cleanup.

## Decision

### 1. PR #295 Merge Pattern: Approve as PR Comment When Self-Authored

**What:** When a branch author cannot approve their own PR (GitHub policy restriction), post the gate decision as a PR comment with clear gate status and reasoning, enabling immediate squash merge without external approval.

**Why:** Fast-track delivery of green CI requires minimizing wait cycles. The gate criteria (CI green, Codecov passing, review threads resolved) are objective and verifiable by the PR author. A commented gate decision is auditable and creates a merge decision record.

**How:**

- Verify all CI checks are SUCCESS
- Verify all automated review threads are resolved
- Post gate decision as PR comment: "✅ **Gate Decision: APPROVED for squash merge**" + rationale
- Execute squash merge with detailed commit message + Copilot co-author trailer

**Scope:** Feature PRs with green CI from `squad/*` branch authors. Does not apply to release PRs (dev→main) or external contributions.

### 2. Duplicate Issue Closure Pattern

**What:** When two issues have identical bodies (exact problem statement, scope, AC, links), close the lower-numbered one in favour of a sprint-stamped variant.

**Why:** Issue number proliferation creates cognitive overhead; sprint-stamped issues (e.g., `[Sprint 19] ...`) are discoverable and prioritized. Keeping the higher-numbered sprint-stamped issue as canonical prevents confusion.

**How:**

- Verify both issues have identical bodies (same problem, not variants)
- Keep the sprint-stamped issue (#293 in this case)
- Close the other issue with reason "not planned" + comment: "Duplicate of #{canonical}. Closing in favour of the sprint-stamped issue."
- Link the closed issue in the canonical issue body or comments if needed

**Scope:** Internal duplicates where one is already sprint-stamped and assigned. Does not apply to user-reported duplicates (those require community communication).

### 3. Sprint Triage Pattern: Title Normalization + Label Cleanup

**What:** When triaging issues into an active sprint, normalize titles to format `[Sprint N] verb(area): description`, set milestone, and remove `go:needs-research` if the body provides sufficient context.

**Why:** Consistent title format makes sprint boards scannable and sortable; removing `go:needs-research` when body is sufficient signals team readiness to proceed (e.g., "investigate caching on MemberRoles" = clear enough to start research).

**How:**

1. `gh issue view {id} --json milestone,title` — verify current state
2. Fix title format if needed: `gh issue edit {id} --title "[Sprint N] verb(area): description"`
3. Set milestone if missing: `gh issue edit {id} --milestone "Sprint N"`
4. Remove `go:needs-research` if body is actionable: `gh issue edit {id} --remove-label "go:needs-research"`
5. Keep `go:needs-research` if body requires investigation (e.g., "auto-fill author on post create" needs Auth state exploration)

**Scope:** Sprint board intake; one-time triage at sprint start.

## Impact

- **Delivery speed:** Green CI → approval comment → squash merge eliminates external approval wait
- **Issue hygiene:** Duplicate closure keeps issue count low and signal-to-noise high
- **Team alignment:** Normalized title format and consistent triage pattern make sprints readable and predictable

## Alternatives Considered

1. **Wait for external approval on PR #295:** Adds unnecessary delay when CI is green and review is resolved.
2. **Keep both issues #293 and #294 open:** Confuses team on which issue to work; adds tracking overhead.
3. **Keep all `go:needs-research` labels:** Makes sprint board harder to interpret; doesn't distinguish between "research incomplete" and "ready to investigate".

## Related Decisions

- Decision #24 (`.squad/` docs on feature branches): Reinforces that squad docs stay on `dev`, not delivery branches
- Decision #23 (Gimli TDD charter): PR #295 confirms that green bUnit tests + UI specialist visual confirmation = complete verification

## Team Notes

No breaking changes. This decision codifies patterns already in use (Boromir's work-check cycle). Squad members should apply these patterns in their own work-check reviews.
