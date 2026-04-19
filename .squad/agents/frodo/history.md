

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
