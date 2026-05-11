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

---

## Session: Sprint 19 Feature Delivery (Round 2 + 3) — 2026-05-11

### Session Summary

Ralph activated for continuous board monitoring. Sprint 19 issues triaged and fully delivered across 3 work-check rounds.

**Issues Resolved:** #293, #296, #299, #300 (all Sprint 19)
**PRs Merged:** #297, #298, #301, #302

### Agents & Issues

- **Sam + Legolas (#293 → PR #297):** ✅ L1+L2 caching for UserManagement Auth0 API calls. `IUserManagementCacheService` + `UserManagementCacheService` (30s L1 / 2min L2). Aragorn squash-merged after all 17 CI checks green.
- **Sam (#296 → PR #298):** ✅ `PostAuthor` value object in `src/Domain/ValueObjects/`. `BlogPost.Author: PostAuthor`, `BlogPostDto` flattened (AuthorId/Name/Email/Roles), `CreateBlogPostCommand` carries PostAuthor. All 221 tests pass.
- **Legolas (#296 → PR #298):** ✅ `Create.razor` removes manual Author input; `AuthenticationStateProvider` injected; auto-populates PostAuthor from Auth0 claims (sub/name/email/roles via `RoleClaimsHelper`). 84 bUnit tests pass.
- **Boromir (#299 → PR #301):** ✅ Pre-push gate source hook + docs alignment. Added `AppHost.Tests` to Gate 5 in `.github/hooks/pre-push`. Updated `CONTRIBUTING.md` gate table. Playbook already correct. Closed duplicate PR #303.
- **Legolas + Sam (#300 → PR #302):** ✅ Edit.razor author ACL. `AuthenticationStateProvider` injected; after loading post, checks user's sub claim vs `post.AuthorId`. Non-owners (non-Admin) redirect to `/blog`. 3 new bUnit tests cover owner/non-owner/Admin scenarios.
- **Aragorn:** Reviewed and merged PRs #297, #298, #301, #302. Authorized issue #300 for Sprint 19 by removing `go:needs-research`. Created follow-up issue #300 from ADR notes. Posted Sprint 19 delivery summary on issue #291.

### Cross-Team Decisions

- **PostAuthor**: Breaking schema change — existing MongoDB blogposts collection needs drop/recreate in dev. Prod migration deferred.
- **AppHost.Tests in pre-push Gate 5**: Source hook already had the fix; installed hook was stale. Running `scripts/install-hooks.sh` refreshes automatically.
- **Edit.razor ACL**: UI-level check for Sprint 19; server-side handler ACL deferred to future sprint.

### Board State at Session End

| Item | Status |
|------|--------|
| Issue #293 | ✅ Closed — PR #297 squash-merged |
| Issue #296 | ✅ Closed — PR #298 squash-merged |
| Issue #299 | ✅ Closed — PR #301 squash-merged |
| Issue #300 | ✅ Closed — PR #302 squash-merged |
| PR #303 | ❌ Closed as duplicate (superceded by PR #301) |

### Blockers & Next Steps

- None — board clear. Sprint 19 complete.
- Follow-up for future sprint: server-side ACL enforcement in `EditBlogPostHandler`.
- PostAuthor schema migration script needed before production deployment.
