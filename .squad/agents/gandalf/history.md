

## Learnings

### PR #5, #6, #7 Security Review — 2026-04-18

**PRs Reviewed:**
- **PR #5:** "ci: add PR build and test workflow" ✅ MERGED
- **PR #6:** "chore: remove Weather and Counter template leftovers" ✅ MERGED
- **PR #7:** "chore: add copyright headers to all .cs files" ✅ MERGED

**PR #5 Security Assessment (CI Workflow):**

Files changed: `.github/workflows/ci.yml`, `.squad/agents/boromir/history.md`

Security findings:
- ✅ **No hardcoded secrets** — All authentication uses GitHub tokens with proper scopes
- ✅ **Minimal RBAC permissions** — `contents:read`, `checks:write`, `pull-requests:write` only
- ✅ **GitHub Actions pinned** — Uses major version pins (@v4, @v1) for supply chain safety
- ✅ **No arbitrary code execution** — Workflow runs only controlled .NET build/test commands
- ✅ **Proper test isolation** — Separate result directories prevent path traversal
- ✅ **CI environment guard** — `CI=true` disables Tailwind in CI (line 43)

**PR #6 Security Assessment (Template Cleanup):**

Files changed: Counter.razor, Weather.razor deleted; RazorSmokeTests.cs modified

Security findings:
- ✅ **Reduced attack surface** — Removed unused routes `/counter`, `/weather`
- ✅ **No authorization bypass** — Deleted components had no auth requirements
- ✅ **Test coverage maintained** — 91.64% line coverage, 74 tests passing
- ✅ **No secrets exposed** — All changes are code deletions only

**PR #7 Security Assessment (Copyright Headers):**

Files changed: 48 C# source files across all projects

Security findings:
- ✅ **Zero functional changes** — Copyright headers are purely cosmetic comments
- ✅ **No secrets or credentials** — No password/key/token keywords found in diffs
- ✅ **Build verification** — All 74 tests passing, 0 errors, 0 warnings
- ✅ **CI checks passing** — build-and-test: SUCCESS (1m16s), Test Results: SUCCESS

**Key Learnings:**

1. **CI/CD Pipeline Security Checklist:**
   - Verify GitHub Actions permissions follow least-privilege principle
   - Check for secrets in workflow files or environment variables
   - Ensure Actions pinned to major versions (not `@latest` or SHA)
   - Review arbitrary code execution risks in workflow steps
   - Validate test isolation (no shared directories)

2. **Attack Surface Reduction Pattern:**
   - Removing unused routes/components reduces potential entry points
   - Ensure deletions don't break dependent code (test coverage crucial)
   - Verify no authorization logic bypassed by removals

3. **Copyright Header Review:**
   - Non-functional changes (comments) still require security review
   - Check for accidental secrets in diff hunks (grep for keywords)
   - Verify CI passes before merge (headers shouldn't break build)
   - Fast rebase workflow: conflicts auto-resolved when files deleted

4. **Post-Merge Validation Process:**
   - Always sync main after merge: `git checkout main && git pull`
   - Build verification: `dotnet build src/Web/Web.csproj --configuration Release`
   - Test verification: `dotnet test --no-restore`
   - Coverage baseline: maintain 91%+ line coverage

5. **Git Rebase for Conflict Resolution:**
   - When PR conflicts with main (e.g., files deleted), use rebase: `git rebase origin/main`
   - Git auto-drops duplicate commits (e.g., PR #7 lost 4 commits already in main)
   - Force-push after rebase: `git push --force-with-lease` to update remote
   - CI re-runs after force-push, ensuring rebased code tested

**Decision Records Created:**
- `.squad/decisions/inbox/gandalf-pr5-pr6-merged.md` (PR #5 & #6)

**Build Workaround:**
- `.slnx` solution build fails with CLR error 0x80131506 (unrelated to PRs)
- Use individual project builds: `dotnet build src/Web/Web.csproj`

### PR #2 Security Audit — 2025-07 (squad/coverage-test-hardening-main)

**Reviewed files:** RoleClaimsHelper.cs, ManageRoles.razor, Profile.razor, Program.cs, AssemblyInfo.cs, TestAuthorizationService.cs, RoleClaimsHelperTests.cs, NavMenu.razor, MainLayout.razor, Home.razor

**Verdict:** REJECT (HIGH finding)

**Key Findings:**

1. **[HIGH] Open Redirect in `/Account/Login`** — `returnUrl` query parameter is passed directly to `WithRedirectUri(returnUrl ?? "/")` in `Program.cs:111` with no local-path validation. An attacker could craft `/Account/Login?returnUrl=https://evil.com` to redirect a user to a phishing site after login. Fix: validate `returnUrl` is a relative/local path before use (e.g., `LocalRedirect` or `Uri.IsWellFormedUriString` check).

2. **[MEDIUM] Potential NullReferenceException in `OnTokenValidated` handler** — `Program.cs:46` captures `options.Events.OnTokenValidated` before the PostConfigure, but Auth0 SDK may set this to `null`. The line `await existingOnTokenValidated(context)` will throw `NullReferenceException` if null, breaking all logins. Fix: guard with `if (existingOnTokenValidated != null)`.

3. **[INFO] Profile page exposes all JWT claims** — The `/profile` page renders all claims including internal ones (`sub`, `auth_time`, `nonce`, etc.) to authenticated users. Not a vulnerability given `[Authorize]` gating, but worth noting for defence-in-depth.

4. **[CLEAN] TestAuthorizationService** — Correctly confined to test project only (`internal sealed`, Tests project only). AssemblyInfo.cs `InternalsVisibleTo` does not leak the service to production.

5. **[CLEAN] Role claim mapping** — RoleClaimsHelper correctly deduplicates and normalizes custom Auth0 claim types to `ClaimTypes.Role`. Logic is sound with no privilege escalation risk.

6. **[CLEAN] No hardcoded secrets** — appsettings files have empty placeholders; secrets are via user-secrets.

7. **[CLEAN] Auth middleware order** — UseAuthentication → UseAuthorization → UseAntiforgery is correct.

8. **[CLEAN] ManageRoles and Profile authorization** — Both pages correctly gated with `[Authorize(Roles = "Admin")]` and `[Authorize]` respectively.

### PR #11 & #12 Security Review — 2026-04-18

**PR #11** — `squad/cleanup-uncommitted-changes` → `dev` (3 files: boromir history, ManageRoles.razor, tailwind.css)

**Verdict:** NEEDS_HUMAN_DECISION

- **[CLEAN] ManageRoles.razor** — Removed redundant `@using MyBlog.Web.Features.UserManagement` (already in `_Imports.razor` per Decision #1). `[Authorize(Roles = "Admin")]` gate remains intact. No security impact.
- **[CLEAN] No secrets** — No credentials or tokens in any changed file.
- **[INFO] Non-minified tailwind.css** — Committed CSS expanded from 1 minified line to 1918 pretty-printed lines with nested CSS syntax. Not a security issue, but the nested `&:` syntax may have browser compatibility implications. Needs Legolas (frontend) to confirm this is intentional and not a build artifact mismatch.

**PR #12** — `squad/prepush-gate` → `dev` (8 files: pre-push hook, install-hooks.sh, CONTRIBUTING.md, SKILL.md, PR template, squad docs)

**Verdict:** APPROVE_READY

- **[CLEAN] Shell script security** — `install-hooks.sh` and `.github/hooks/pre-push` use proper variable quoting, no eval/exec of user input, `set -e` / `set -uo pipefail`, and no injection vectors.
- **[CLEAN] No secrets** — No credentials committed. PR template checklist correctly includes secrets check.
- **[LOW] Shebang portability** — `install-hooks.sh:1` uses `#!/bin/bash`; pre-push hook uses `#!/usr/bin/env bash`. Minor inconsistency, not a security issue.
- **[LOW] Stale Azurite reference** — `pre-push:116` mentions Azurite but only MongoDB Testcontainers are used. Misleading, not dangerous.
- **[LOW] Dead playbook link** — `SKILL.md:17,75` references `.squad/playbooks/pre-push-process.md` which does not exist.

### 2026-04-18 — PR #11 & #12 Security Review (Final Summary)

- Completed security reviews for PR #11 (cleanup-uncommitted-changes) and PR #12 (prepush-gate)
- PR #11 verdict: NEEDS_HUMAN_DECISION (pending Legolas CSS confirmation); approved from security
- PR #12 verdict: APPROVE_READY (shell security clean, minor non-blocking issues)
- All findings recorded in session history; decisions consolidated by Scribe
- Orchestration log created in `.squad/orchestration-log/2026-04-18T17-05-49-gandalf.md`

### PR #16 Security Review — 2026-04-19

**PR:** `squad/1001-sprint-1-1` → `dev` (30 files: shell hooks, install script, squad skills, routing, integration tests)

**Verdict:** SECURITY APPROVED

**Key Findings:**

1. **[CLEAN] Shell Script Security** — All three hook-related files (`.github/hooks/pre-push`, `.github/hooks/post-checkout`, `scripts/install-hooks.sh`) use proper variable quoting, `set -e` error handling, safe `git rev-parse` path discovery, and no eval/exec of user input.

2. **[CLEAN] Gate 0 Branch Regex** — New enforcement pattern `^squad/[0-9]+-[a-z0-9-]+$` is a strict allowlist with no injection vectors.

3. **[CLEAN] No Secrets** — No credentials, tokens, or API keys in any changed file. New Auth0 skills correctly document secrets management via User Secrets and CI environment variables only.

4. **[CLEAN] Auth0 Skills** — Both `.squad/skills/auth0-management-api/SKILL.md` and `.squad/skills/auth0-management-security/SKILL.md` correctly document:
   - Least-privilege scope guidance
   - AdminPolicy enforcement boundary
   - Secrets-never-committed rule

5. **[CLEAN] Routing Table** — Skill injection rules in `.squad/routing.md` correctly reference auth0-management-security skill for security audits.

6. **[CLEAN] No Auth Changes** — No modifications to `appsettings*.json`, `Program.cs`, or any authorization pipeline code.

**CI Status:** 7/8 checks passed (Test Results, Coverage Summary, Unit Tests, Architecture Tests, Integration Tests, build-and-test, Prepare); Agent check still in progress (non-blocking).

Posted security approval comment to PR #16.

### PR #16 Merge to dev — 2026-04-19

**PR:** squad/1001-sprint-1-1 → dev (30 files: hooks, install script, skills, routing, integration tests)

**Verdict:** SECURITY APPROVED

**Key Findings (Final):**

1. **[CLEAN] Shell Script Security** — All three hook-related files use proper variable quoting, `set -e` error handling, safe `git rev-parse` path discovery, and no eval/exec of user input.

2. **[CLEAN] Gate 0 Branch Regex** — Enforcement pattern `^squad/[0-9]+-[a-z0-9-]+$` is a strict allowlist with no injection vectors.

3. **[CLEAN] No Secrets** — No credentials, tokens, or API keys in any changed file. New Auth0 skills correctly document secrets management via User Secrets and CI environment variables only.

4. **[CLEAN] Auth0 Skills** — Both skills correctly document least-privilege scope guidance, AdminPolicy enforcement boundary, and secrets-never-committed rule.

5. **[CLEAN] Routing Table** — Skill injection rules correctly reference auth0-management-security skill for security audits.

6. **[CLEAN] No Auth Changes** — No modifications to `appsettings*.json`, `Program.cs`, or any authorization pipeline code.

**CI Status:** 7/8 checks passed (Agent check non-blocking).

**Cross-team:** Aragorn merged PR #16 to dev with non-destructive integration. Local dev now ahead of origin/dev by 5 commits. Sprint 1.1 complete.

**Orchestration Log:** `.squad/orchestration-log/2026-04-19T13:26:36Z-gandalf.md`
### PR #17 Security Review — 2026-04-19

**PR #17** — `squad/1002-boromir-history-update` → `dev` (29 files: skills docs, playbooks, agent histories)

**Verdict:** APPROVE ✅

**Scope:** All changes confined to `.squad/` directory — documentation only, no feature code.

**Security Checks:**
- **[CLEAN] No hardcoded secrets** — Secret references in skill docs are environment variable names only (GITHUB_TOKEN, NUGET_API_KEY), not values
- **[CLEAN] Auth guidance correct** — Auth0 skills correctly emphasize user-secrets for local dev, GitHub Actions secrets for CI
- **[CLEAN] No sensitive file changes** — src/, appsettings, Program.cs unaffected

**Merge Conflict Resolution:**
Resolved 7 add/add conflicts in `.squad/skills/` by accepting `origin/dev` versions (Sprint 2 mining adaptations with MyBlog-specific paths and ownership rules):
- auth0-management-api/SKILL.md
- auth0-management-security/SKILL.md
- mongodb-dba-patterns/SKILL.md
- mongodb-filter-pattern/SKILL.md
- release-process/SKILL.md
- testcontainers-shared-fixture/SKILL.md
- webapp-testing/SKILL.md

**Learning:** Add/add conflicts in skill files result from parallel imports. The `origin/dev` versions are authoritative when adapted for MyBlog conventions (file paths, ownership rules, real examples).
