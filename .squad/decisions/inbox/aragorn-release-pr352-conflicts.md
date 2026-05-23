## 2026-05-23 — Release PR #352 conflict resolution baseline

**Context:** `dev` → `main` release PR #352 hit add/add conflicts in the AppHost Mongo dev-command files after Sprint 19 landed on `dev` while Sprint 18 release changes already existed on `main`.

**Decision:** Keep the `dev` versions of the conflicted AppHost source/test files as the authoritative resolution, then merge `main` into `dev` to preserve branch ancestry.

**Why:**

- `dev` contains the newer Sprint 19 behavior (category-aware seed data plus deterministic AppHost concurrency/runtime tests).
- `main` contains older add/add versions of the same files, so taking `main` would regress release-ready functionality.
- The release goal is branch convergence without backing out validated `dev` behavior.

**Files resolved this way:**

- `src/AppHost/MongoDbResourceBuilderExtensions.cs`
- `tests/AppHost.Tests/MongoSeedDataIntegrationTests.cs`
- `tests/AppHost.Tests/MongoShowStatsIntegrationTests.cs`
