# Project Context

- **Project:** MyBlog
- **Created:** 2026-04-17

## Core Context

Agent Scribe initialized and ready for work.

## Recent Updates

📌 Team initialized on 2026-04-17

## Learnings

Initial setup complete.

---

## Session: 2026-05-08 — Sprint 15 MongoDB Clear Command (Full Vertical Slice)

**Team Outcome:** ✅ Feature slice complete (tracer bullet); 🔄 hardening in progress

### Agents & Issues Completed

- **Aragorn (#246):** ✅ PRD audit complete — Issue #246 closed as satisfied; routing validated to #247-#249
- **Boromir (#247):** ✅ AppHost wiring complete — `clear-myblog-data` command exposed, local-only, health-gated, confirmation-required
- **Sam (#248):** ✅ Clearing logic implemented — Real `DeleteManyAsync` collection clearing with structured results
- **Gimli (#247 AC4):** 🔄 Test coverage written but RED — Awaiting Boromir's wiring validation
- **Boromir (#249):** 🚀 Hardening launched — Reentrancy, best-effort resilience, live-clear; depends on Gimli clearance

### Cross-Team Decisions

1. **Decision #20:** PRD issues close upon spec completion; implementation routes to scoped slices (#247-#249)
2. **Decision #21:** AppHost command gating pattern: `IsRunMode`, health-gate, confirmation-required; tracer-bullet handler
3. **Decision #22:** Collection clearing via `DeleteManyAsync` (preserves indexes/schema); connection string via `ConnectionStringExpression.GetValueAsync()`

### Artifacts Created

- **Session Log:** `.squad/log/2026-05-08T04:57:35Z-sprint15-mongo-clear.md`
- **Orchestration Logs:** 5 agent logs in `.squad/orchestration-log/`
- **Merged Decisions:** 3 inbox files merged into `decisions.md` (#20, #21, #22)

### Blockers & Next Steps

- **Gimli:** Resolve pre-existing `CustomResourceSnapshot` init setter issue; align command name `"clear-myblog-data"`
- **Boromir:** Proceed with #249 hardening after Gimli validates coverage (tests turn GREEN)

---

## Session: 2026-05-08 — Ralph Board Sweep: Release Labeling, Mutex Rename, CI Failures Filed

**Triggered by:** Ralph (Work Monitor) — "Ralph, go" autonomous board sweep
**Team Outcome:** ✅ Issue #265 closed (release-candidate label applied); 🔄 PR #267 open targeting `dev`; 🆕 Issues #268 and #269 filed for Boromir

### Agents & Issues

- **Ralph (#265):** ✅ Milestone review — decided Option A (release candidate, minor version bump to v1.5.0). Rationale: PRs #259 (`WithClearDatabaseCommand`) and #260 (`WithSeedDataCommand`) are additive user-facing enhancements, no breaking changes, CI green. Applied `release-candidate` label, removed `pending-review`, commented decision on issue. Issue auto-closed by `milestone-blog.yml` automation.
- **Sam (#266):** ✅ Refactor rename complete — created branch `squad/266-rename-clear-mutex-to-db-mutex`,
  renamed `_clearMutex → _dbMutex` across 7 sites in `src/AppHost/MongoDbResourceBuilderExtensions.cs`
  (1 declaration + 6 usage sites + 1 comment updated). Pre-push gates green: build 0 errors,
  Architecture.Tests 15/15, Domain.Tests 42/42, Integration.Tests 12/12.
  PR #267 opened targeting `dev`, Copilot review requested.
- **Ralph (CI triage):** 🆕 Filed Issue #268 — `squad-mark-released.yml` fails with GraphQL permission error (`GITHUB_TOKEN` lacks `project` scope for ProjectV2 queries; fix: `PROJECT_TOKEN` PAT secret). Filed Issue #269 — Blog→README Sync workflow fails because direct push to `main` is blocked by branch ruleset (fix: PR-based approach via `sync/*` branch). Both labeled `squad:boromir,bug`.

### Cross-Team Decisions

None — no new patterns or conventions introduced this session. This was a release-labeling and refactor-rename sweep.

### Board State at Session End

| Item | Status |
|------|--------|
| Issue #265 | ✅ Closed — `release-candidate` label applied; auto-closed by `milestone-blog.yml` |
| Issue #266 | ✅ Closed — resolved by PR #267 |
| PR #267 | 🔄 Open, targeting `dev`, awaiting merge |
| Issue #268 | 🆕 Filed for Boromir — Squad Mark Released CI GraphQL permission fix |
| Issue #269 | 🆕 Filed for Boromir — Blog→README Sync CI direct-push-to-main fix |

### Blockers & Next Steps

- **Boromir:** Fix CI issues #268 (add `PROJECT_TOKEN` secret, update `squad-mark-released.yml`) and #269 (PR-based sync workflow for `main`)
- **PR #267:** Awaiting reviewer merge to `dev`
