## Learnings

### 2026-04-18 — PR #9 Review Fixes: Workflow Hardening

**Issues addressed:**

1. **SDK version conflict (`dotnet-quality: 'preview'` vs `global.json allowPrerelease: false`)**
   - `global.json` pins SDK `10.0.100` with `allowPrerelease: false`
   - Workflows were using `dotnet-version: '10.0.x'` + `dotnet-quality: 'preview'` which contradicts the pin
   - Fix: replace with `global-json-file: global.json` in all 3 setup-dotnet steps (ci.yml + 3 in squad-test.yml)
   - Lesson: always use `global-json-file` when `global.json` exists — avoids version drift and preview SDK pollution

2. **`assemblySemVer` drops prerelease labels**
   - `assemblySemVer` is Major.Minor.Patch only (e.g., `1.0.0`) — strips prerelease suffixes
   - `nuGetVersion` includes prerelease labels (e.g., `1.0.0-alpha.1`) per NuGet conventions
   - Fix: use `nuGetVersion` for `/p:Version` and add `informationalVersion` for full metadata
   - Lesson: `assemblySemVer` ≠ package version; always use `nuGetVersion` for /p:Version in CI builds

3. **Duplicate CI triggers between workflows**
   - Both `ci.yml` and `squad-test.yml` had push triggers to main/dev, causing doubled CI runs on push
   - Fix: removed `push` trigger from `squad-test.yml` — it's a PR-only parallel test workflow
   - Lesson: parallel test workflows should be PR-scoped; push coverage belongs in ci.yml

4. **`continue-on-error` too broad on coverage steps**
   - Coverage generation failing silently (continue-on-error) hides real problems
   - Only the PR *comment posting* step should be optional (can't always post to PRs)
   - Fix: removed `continue-on-error` from download-artifact and ReportGenerator steps; kept only on sticky-pr-comment
   - Lesson: `continue-on-error: true` should be surgical — only on truly optional steps like notifications

5. **Misleading comment in squad-issue-assign.yml**
   - Comment said "Get the default branch name (main, master, etc.)" but code hardcoded `baseBranch = 'dev'`
   - Fix: replaced with "Base branch for squad PRs"
   - Lesson: comments that contradict the code are worse than no comments — they mislead future readers

**Commits pushed to squad/cicd-phase3-4:**
- `173f3e14` — ci.yml: global.json SDK + nuGetVersion + InformationalVersion
- `2d2efdcd` — squad-test.yml: global.json SDK + remove push trigger + tighten continue-on-error
- `1d054a4b` — squad-issue-assign.yml: fix misleading base branch comment
### 2025-01-29 — CI/CD Phase 3-4: Parallel Tests, GitVersion, Dev Branch Strategy

**Work completed:**
- Created `dev` branch from main and set as default branch (GitHub API)
- Applied branch protection rules via gh api:
  - `main`: strict checks, 1 approval required, only accepts PRs from dev or hotfix/*
  - `dev`: CI required, flexible reviews (0 approvals), primary development branch
- Updated `.github/workflows/ci.yml`: Added `dev` branch trigger + GitVersion step for SemVer stamping
- Created `.github/workflows/squad-test.yml`: 3 parallel test jobs (Architecture, Unit, Integration) with coverage aggregation and sticky PR comment
- Created `.github/workflows/hotfix-backport-reminder.yml`: Auto-comment on hotfix merges to main reminding team to backport to dev
- Fixed `.github/workflows/squad-issue-assign.yml`: Hardcoded `dev` as base branch (was using dynamic default_branch)

**Branch strategy implemented:**
- `dev` is now the default branch and primary development target
- `main` becomes release-only branch (strict protection)
- Feature/squad branches → PR to `dev` → merge to `dev`
- Dev → main promotion requires PR + 1 approval
- Hotfixes can go directly to main, but auto-reminder triggers to backport to dev

**GitVersion integration:**
- GitVersion.yml already existed at repo root with full branch config (main/dev/feature/squad/pull-request)
- Added GitVersion steps to ci.yml: setup → execute → display SemVer
- Build now stamps assemblies with GitVersion-calculated versions: Version, AssemblyVersion, FileVersion

**Parallel test workflow features:**
- Version job runs GitVersion once, outputs used by all test jobs
- 3 test jobs run in parallel: Architecture, Unit, Integration
- Each job uploads test results (TRX) and coverage (cobertura.xml)
- Coverage-report job aggregates all coverage files using ReportGenerator
- Sticky PR comment posts coverage summary (no spam)
- All coverage steps use `continue-on-error: true` to prevent failures if no coverage files

**Key decisions:**
- Used `fetch-depth: 0` for GitVersion (requires full git history)
- Version job outputs semVer and assemblySemVer for downstream jobs
- Hardcoded `dev` in squad-issue-assign.yml to ensure Copilot agent uses correct base branch
- Applied branch protection immediately after creating dev branch
- Used GitVersion action versions: `gittools/actions/gitversion/setup@v3` and `gittools/actions/gitversion/execute@v3`

**PR created:**
- PR #9: `squad/cicd-phase3-4` → `dev`
- All workflow changes committed in single commit
- Branch protection rules applied before PR creation

**Next steps:**
- Monitor PR #9 CI run to verify GitVersion integration and parallel tests
- Consider adding `squad-promote.yml` workflow for dev → main releases
- May need to adjust coverage thresholds based on aggregated coverage data

### 2025-01-29 — PR #4 Review: Razor @using Consolidation

**Review process for application code PRs:**
- Always check for `.squad/` file modifications — they are only allowed in `squad/*` branches
- Verify branch name matches pattern before flagging protected branch violation
- Build verification is critical for Razor changes — missing usings are immediately caught by Razor compiler
- Run full test suite (Architecture + Unit + Integration) to ensure no regressions

**What was reviewed:**
- PR #4 from Legolas: consolidated 14 redundant `@using` directives from 9 Razor files into `Features/_Imports.razor`
- Added `@using Microsoft.AspNetCore.Authorization` and `@using MediatR` to Features/_Imports.razor
- Branch: `squad/razor-imports-consolidation` (permitted .squad/ changes)

**Build & test results:**
- Build: ✅ 0 errors, 0 warnings
- Architecture.Tests: ✅ 6 passed
- Unit.Tests: ✅ 61 passed (92.04% coverage)
- Integration.Tests: ✅ 9 passed
- All tests: ✅ 76/76 passing

**Action taken:**
- Merged PR #4 with squash + deleted branch
- Verified main branch updated successfully

### 2025-01-29 — Created CI/CD Workflow for Pull Requests

**Project context:**
- MyBlog solution uses .NET 10 (net10.0) with .slnx format (not .sln)
- Three test projects: Architecture.Tests (6 tests), Unit.Tests (61 tests, 92.04% coverage), Integration.Tests (9 tests)
- Unit tests use coverlet.msbuild with cobertura output format and 89% threshold
- Integration tests use Testcontainers.MongoDb for MongoDB dependencies
- Web project has custom Tailwind build target that skips when CI=true

**Workflow features built:**
1. **Triggers** — pull_request to main/squad/**, push to main
2. **Build** — dotnet build MyBlog.slnx in Release mode with CI=true env var
3. **Test execution** — all three test suites with TRX output + code coverage
4. **Test reporting** — dorny/test-reporter@v1 for inline PR annotations
5. **Coverage reporting** — irongut/CodeCoverageSummary@v1.3.0 for PR comments
6. **NuGet caching** — caches ~/.nuget/packages keyed by csproj + Directory.Packages.props

**Key decisions:**
- Used .NET 10.x preview quality in setup-dotnet (global.json specifies 10.0.100)
- Set CI=true environment variable for build to skip Tailwind compilation
- Ran tests with --no-build to avoid double compilation
- Used separate test result directories per suite for clean artifact organization
- Applied permissions: contents:read, checks:write, pull-requests:write
- Used marocchino/sticky-pull-request-comment for coverage to avoid comment spam

**Integration test considerations:**
- Testcontainers.MongoDb package detected — no special CI setup needed
- GitHub Actions runner has Docker pre-installed, Testcontainers will pull images automatically
- No external MongoDB service configuration required

**Files created:**
- `.github/workflows/ci.yml` — full CI pipeline with build, test, coverage

**Next steps:**
- Monitor first workflow run on PR #5 to verify all steps execute successfully
- May need to adjust coverage thresholds or exclusions based on actual coverage data
- Consider adding caching for Docker images if Testcontainers startup becomes slow

### 2025-01-29 — IssueTrackerApp Workflow Analysis for MyBlog CI/CD Strategy

**Reviewed workflows:**
1. squad-ci.yml — fast PR validation (build only, no tests)
2. squad-test.yml — comprehensive test suite with multiple jobs (Domain, Web, Architecture, bUnit, Integration, MongoDB, Azure, Aspire+Playwright)
3. squad-promote.yml — workflow_dispatch to open dev → main release PR using GitVersion
4. squad-pr-auto-label.yml — auto-labels PRs with `squad` or `squad:boromir` (for bots)
5. squad-label-enforce.yml — enforces mutual exclusivity for go:, release:, type:, priority: labels
6. squad-triage.yml — auto-assigns issues with `squad` label to team members
7. squad-heartbeat.yml — 15-min cron + issue/PR events to keep board active, auto-assign @copilot
8. squad-issue-assign.yml — applies squad:{member} label and posts assignment comment
9. codeql-analysis.yml — security scanning (weekly + PR + push to main)
10. code-metrics.yml — generates CODE_METRICS.md with dotnet/samples action
11. squad-milestone-release.yml — workflow_dispatch to tag and publish GitHub Release
12. sync-squad-labels.yml — syncs squad labels from .squad/team.md

**Key patterns observed:**
- **Branch strategy:** IssueTrackerApp uses dev/preview/insider/main with dev as primary development branch, main as release-only
- **Test parallelization:** squad-test.yml runs 10+ test jobs in parallel with build artifact caching
- **MongoDB setup:** Uses docker run mongo:7.0 with replica set initialization for integration tests (EF Core transactions)
- **Testcontainers:** Used for Azurite (Azure Storage emulator) — no manual docker setup
- **Coverage:** ReportGenerator aggregates all coverage.cobertura.xml files, enforces 80% threshold
- **GitVersion:** Uses GitVersion.yml for semantic versioning on promote workflow
- **Squad automation:** Label-based triage, routing, and auto-assignment to @copilot coding agent
- **Aspire tests:** AppHost.Tests uses Playwright for E2E testing (45-min timeout, installs chromium)
- **Concurrency:** squad-test.yml uses cancel-in-progress for same ref
- **Secrets:** Auth0 and MongoDB connection strings passed as env vars

**Dev/main branch strategy insights:**
- squad-ci.yml triggers on PR to dev/preview/main/insider and push to dev/insider
- squad-promote.yml opens PR from dev → main with GitVersion-calculated version
- squad-milestone-release.yml runs on main to create GitHub Release tag
- squad-heartbeat.yml uses baseBranch = 'dev' for @copilot agent assignments
- squad-issue-assign.yml uses baseBranch = 'dev' for @copilot agent assignments

**Recommendations for MyBlog:**
1. **Adopt immediately:** squad-triage.yml, squad-pr-auto-label.yml, squad-label-enforce.yml, sync-squad-labels.yml, codeql-analysis.yml
2. **Adapt for MyBlog stack:** squad-test.yml (remove MongoDB/Azure persistence jobs, keep Architecture/Unit/Integration/Blazor tests)
3. **Dev branch strategy:** Requires creating dev branch as default branch, main becomes release-only
4. **Coverage strategy:** Adopt ReportGenerator + 80% threshold pattern from squad-test.yml
5. **Skip:** squad-heartbeat.yml (Ralph agent), squad-issue-assign.yml (@copilot auto-assign), squad-milestone-release.yml (not using GitVersion), code-metrics.yml (low priority)
6. **Gap analysis:** MyBlog lacks parallel test execution, MongoDB integration test setup, Aspire E2E tests, dev→main promotion workflow

