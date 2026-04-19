

## Learnings

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
