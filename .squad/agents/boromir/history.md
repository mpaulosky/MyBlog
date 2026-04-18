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
