# Deleted Squad Assets — MyBlog Asset Catalog

**Last Updated:** 2026-04-19  
**Owner:** Pippin (Docs)  
**Status:** Published

## Overview

This manifest records squad skills and playbooks that were imported during the adoption process but determined to be non-fit for MyBlog after evaluation. Assets listed here were explicitly deleted rather than archived, reflecting the project's commitment to maintaining a lean, intentional skill and playbook catalog.

## Deleted Skills

### 1. post-build-validation

**Removed:** Sprint 3 (Milestone 3)  
**Original Imported From:** Generic squad template library  
**Decision:** [DELETE](#decision-post-build-validation)

**Reason:**

The post-build-validation skill was designed for projects requiring external state verification after build completion—specifically, game-world validation patterns (e.g., RCON command verification after structure placement). MyBlog has no remote operations, external API validation hooks, or out-of-band state checks.

The pattern's core premise (graceful degradation when remote validation fails) does not apply to MyBlog's build process, where test failures must always block the build. Importing this skill would introduce conceptual overhead without operational value.

**Related Decisions:** [Decision 9: Secondary Skill Fit Assessment](#secondary-skill-fit-assessment)

---

### 2. static-config-pattern

**Removed:** Sprint 3 (Milestone 3)  
**Original Imported From:** Generic squad template library  
**Decision:** [DELETE](#decision-static-config-pattern)

**Reason:**

The static-config-pattern skill documents a backwards-compatible refactoring pattern: converting C# const fields into static properties to enable runtime configuration changes in legacy projects. MyBlog already adopts ASP.NET Core's idiomatic approach—`IConfiguration` + Options pattern—for all configurable values.

Infrastructure constants (health endpoint path, cache key prefix) are deliberately const; they are not configuration debt. No business case for the pattern exists, and including it would only confuse future contributors about MyBlog's configuration model.

**Related Decisions:** [Decision 9: Secondary Skill Fit Assessment](#secondary-skill-fit-assessment)

---

### 3. building-protection

**Removed:** Sprint 3 (Milestone 3)  
**Original Imported From:** Generic squad template library  
**Decision:** Delete in final disposition pass

**Reason:**

The building-protection skill is Minecraft-world guidance for clipping `/fill`
commands around protected building volumes. MyBlog has no voxel world state,
bounding-box excavation workflow, or analogous subsystem collision problem.

Milestone 1 intentionally kept the file quarantined only to prevent accidental
injection before Sprint 3. No later decision granted it an active MyBlog use
case, so the final cleanup pass removes it instead of carrying forward dead
quarantine context.

**Related Decisions:** Guardrail routing quarantine (Milestone 1); final
Sprint 3 disposition

---

### 4. release-process-base

**Removed:** Sprint 3 (Milestone 3)  
**Original Imported From:** Generic squad template library  
**Decision:** Delete in final disposition pass

**Reason:**

The release-process-base skill was a generic upstream template kept only as
temporary quarantine context while MyBlog-specific release guidance was being
written. That rewrite is now complete through
`.squad/skills/release-process/SKILL.md` and
`.squad/playbooks/release-myblog.md`.

Because MyBlog now has an explicit release path, retaining the generic base
skill would only preserve misleading non-repo-specific guidance. The release
guidance fit review already said Sprint 3 could safely delete it unless a new
template use case was approved, and no such keep decision exists.

**Related Decisions:** Release guidance fit review; final Sprint 3 disposition

---

## Deleted Playbooks

### 1. release-issuetracker

**Removed:** Sprint 3 (Milestone 3)  
**Original Imported From:** IssueTrackerApp release workflow  
**Decision:** Replaced by MyBlog-specific release playbook

**Reason:**

The imported release playbook assumed IssueTrackerApp-specific automation and
branch behavior that do not exist in MyBlog. It was superseded by
`.squad/playbooks/release-myblog.md`, which reflects the real `dev` → `main`
and `hotfix/*` workflow in this repository.

**Related Decisions:** Release guidance fit review

---

## Retained & Rewritten Assets

The following assets were evaluated but retained after adaptation:

### microsoft-code-reference

**Status:** Retained; scoped rewrite pending  
**Owner:** Boromir  
**Rationale:** Reference skill (not code pattern). Applicable to CI/CD troubleshooting, NuGet verification, and GitHub Actions pattern discovery. Rewrite will clarify scope for DevOps/NuGet/GitHub Actions workflows.

---

## Reference: Decision 9 — Secondary Skill Fit Assessment

| Skill | Fit | Decision | Reason |
|-------|-----|----------|--------|
| **post-build-validation** | ❌ Poor | DELETE | Pattern designed for external game-world state validation (RCON block verification). MyBlog has no remote operations. Test failures **must** block build. |
| **static-config-pattern** | 🟡 Marginal | DELETE | Backwards-compatible const→static refactor. MyBlog uses ASP.NET Core `IConfiguration` + Options pattern. No current business case. |
| **building-protection** | ❌ Poor | DELETE | Minecraft-only world-building guard. Kept temporarily as quarantine context, but no MyBlog use case was approved before Sprint 3 cleanup. |
| **microsoft-code-reference** | ✅ Good | RETAIN & CLARIFY | Reference skill (tools + query patterns). Applicable to CI/CD troubleshooting, NuGet verification, GitHub Actions. Needs rewrite for DevOps scope. |
| **release-process-base** | ❌ Poor | DELETE | Generic release template kept only until MyBlog-specific release guidance existed. Replaced by `release-process` + `release-myblog`. |

---

## How to Reference This Manifest

When a contributor asks why a particular skill or playbook was not imported or
was removed, refer them to the relevant entry above. The manifest is the source
of truth for MyBlog's asset disposition decisions.

**Future Deletion Submissions:** New deletions should be added to this manifest with the same structure (asset name, removal sprint, reason, related decision).
