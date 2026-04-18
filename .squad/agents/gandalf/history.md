

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
