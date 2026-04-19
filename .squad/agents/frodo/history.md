

## 2026-04-19 — Admin Role Claim Namespace Fix (Cross-Agent with Legolas)

**Work:** Diagnosed and fixed admin role claim mismatch between Auth0 and app configuration.

**Root Cause:** Auth0 sends roles under `https://articlesite.com/roles` but app only recognized `https://myblog/roles`.

**Implementation:**
- Updated `RoleClaimsHelper` to infer role claim types from any claim type ending in `role` or `roles`
- Updated `appsettings.json` to list known Auth0 namespaces for reference
- Ensured role claim resolution works regardless of namespace variations

**Impact:** 
- Role claims now correctly normalized and available to authorization checks
- Frontend (Legolas) validated UI correctly displays admin role in Profile and NavMenu

**Status:** ✅ Completed — Decision merged to decisions.md

## 2026-04-19 — CONTRIBUTING.md Pre-Push & PR Sections (Skills Review)

As part of DevOps skills/playbooks review, Frodo assigned to update CONTRIBUTING.md with pre-push validation gates and PR review process.

**Action:** Add two new sections to CONTRIBUTING.md (1h):
1. Pre-Push Validation Gates — link to playbook, list 5 gates, quick checklist
2. PR Review Process — link to pr-merge-process playbook, explain rejection protocol

**Collaboration:** Frodo + Pippin (CONTRIBUTING.md co-owners).

**Timeline:** Week 1 (1h estimated).

## 2026-04-19 — Auth0 Secrets Policy & Documentation Consolidation (Sprint 2)

**Backlog item:** Tighten and clarify the repo's Auth0 secrets policy across documentation.

**Initial work completed:**
1. Updated SECURITY.md with Auth0 secrets policy section
2. Updated SKILL.md documentation references
3. Created decision inbox file

**First correction pass:**
- Fixed Data Protection, API Security, MongoDB Security sections
- Removed SQL/EF Core references; added verified Aspire/MongoDB/Blazor claims
- Cleaned up Known Limitations

**Final cleanup pass (second correction):**
- **API Security** — Removed "Input validation - All CQRS commands validate user input" (unverifiable blanket claim; validation exists in domain models but not uniformly)
- **Data Validation** — Replaced "Use parameterized queries (Entity Framework Core does this automatically)" with MongoDB-grounded alternative: "Validate all user input at the domain model level (e.g., `ArgumentException.ThrowIfNullOrWhiteSpace`)"
- **Secrets Management** — Removed unverifiable "Never commit `appsettings.Production.json` with secrets" and ".gitignore" reference; kept only grounded guidance about User Secrets / Environment Variables

**Factual correction (third pass):**
- **API Security → Error handling** — Changed "Auth0 errors wrapped and logged" to "Auth0 errors wrapped in Result objects" (verified UserManagementHandler.cs does NOT log; only wraps with Result.Fail)

**Final state:**
✅ SECURITY.md is now fully repo-grounded with only verifiable, MyBlog-specific claims
✅ Auth0 secrets policy work intact and emphasized (§ "Auth0 Secrets Management Policy")
✅ No EF Core / SQL / generic security marketing language remaining
✅ Sprint 2 backlog item complete and accurate

**Status:** ✅ Final - Sprint 2 backlog closed; ready for Scribe merge.
