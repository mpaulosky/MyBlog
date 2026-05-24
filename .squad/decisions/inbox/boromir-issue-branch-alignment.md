### 2026-05-24: Issue branch alignment before file edits

**By:** Boromir
**What:** Added a reusable issue-branch-alignment skill and routed it as a start-of-work guardrail so issue files are rehomed onto the correct `squad/{issue}-{slug}` branch before commit/push work continues.
**Why:** Issue #371 work was started dirty on `dev`. Capturing the stash-and-rehome flow as a required process reduces future mixed-branch PRs and keeps `dev` recoverable.
