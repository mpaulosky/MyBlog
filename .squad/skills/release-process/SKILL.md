---
name: "release-process"
description: "⚠️ LEGACY/DEPRECATED. This skill contains outdated release patterns for BlazorWebFormsComponents (upstream fork). Use `.squad/skills/release-process-base/SKILL.md` for generic patterns or `.squad/playbooks/release-issuetracker.md` for IssueTrackerApp-specific steps."
domain: "release-workflow"
confidence: "low"
status: "deprecated"
source: "legacy"
---

## ⚠️ This Skill Is Deprecated

This skill contains project-specific release processes from **BlazorWebFormsComponents** and is no longer the primary reference for release work on this project.

### Why Deprecated?

- Designed for a different repository (upstream fork `FritzAndFriends/BlazorWebFormsComponents`)
- Does not reflect IssueTrackerApp's release model (single-branch, NBGV, minimal artifacts)
- Overlaps with the new generic skill and project playbook (see below)

### What to Use Instead

**For generic release workflow patterns (any project):**
→ `.squad/skills/release-process-base/SKILL.md`
- Framework-agnostic versioning strategies (static file, NBGV, tag-only)
- Two-branch vs. single-branch models
- Merge strategies and CI/CD architecture
- Common troubleshooting

**For IssueTrackerApp-specific release steps:**
→ `.squad/playbooks/release-issuetracker.md`
- Single-branch model (all work on `main`)
- NBGV version management
- Step-by-step release commands
- IssueTrackerApp-specific CI/CD configuration

### Can This Be Deleted?

Yes, after all old references to this skill are cleaned up and team members migrate to the new resources. Track cleanup in issues or decisions; deletion is safe once migration is complete.

---

**Last Updated:** 2026-04-13  
**Deprecated By:** Frodo (Tech Writer)  
**Replacement Strategy:** Generic skill + project playbook
