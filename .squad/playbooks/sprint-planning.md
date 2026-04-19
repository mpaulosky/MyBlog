# Sprint Planning Playbook

**Owner:** Ralph (decomposition) + Aragorn (GH artifacts) + Boromir (worktrees)
**Ref:** `.squad/ceremonies.md` (Sprint Planning ceremony)
**Last Updated:** 2026-04-19

---

## Overview

This playbook converts a `plan.md` into a full GitHub tracking structure:
one milestone per sprint, issues per task, a project board entry per issue,
and an isolated git worktree per sprint.

**Trigger:** Auto — runs whenever `plan.md` is created or materially updated.

| Agent | Responsibility |
|-------|---------------|
| Ralph | Reviews plan, decomposes into sprints, triggers ceremony |
| Aragorn | Creates GH milestones, issues, project assignments, sprint close PR |
| Boromir | Creates sprint branches and worktrees, tears down after sprint close |

---

## Step 1 — Ralph: Decompose the Plan

Read `plan.md` and the SQL `todos` table. Group todos into logical sprints:

- **By dependency:** todos that must precede others → earlier sprints
- **By theme:** related work (e.g., all auth tasks) in one sprint
- **By size:** target 3–6 issues per sprint

Name each sprint with a short descriptive theme:

| Sprint | Example Theme |
|--------|--------------|
| 1 | Foundation |
| 2 | Auth & Security |
| 3 | UI Polish |
| 4 | Performance & Observability |

Update `plan.md` with a Sprint Breakdown section before proceeding:

```markdown
## Sprint Breakdown
- **Sprint 1 — Foundation:** todo-1, todo-2, todo-3
- **Sprint 2 — Auth:** todo-4, todo-5
```

---

## Step 2 — Aragorn: Create GitHub Milestones

One milestone per sprint. Create via GitHub API:

```bash
gh api repos/mpaulosky/MyBlog/milestones \
  -f title="Sprint N: {Theme}" \
  -f description="{Sprint goal in one sentence}" \
  -f due_on="{YYYY-MM-DDT00:00:00Z}" \
  -f state="open"
```

**Naming convention:** `Sprint {N}: {Theme}` (e.g., `Sprint 1: Foundation`)

Note the milestone number returned — you will need it for Step 5 and Step 7.

---

## Step 3 — Aragorn: Create GitHub Issues

One issue per todo/unit of work within each sprint:

```bash
gh issue create \
  --title "[Sprint N] {Verb} {Noun}" \
  --milestone "Sprint N: {Theme}" \
  --label "squad" \
  --body "## Goal
{Description from plan.md todo}

## Acceptance Criteria
- [ ] {criterion 1}
- [ ] {criterion 2}

## Sprint
Sprint N: {Theme}

## Plan Reference
Todo ID: {todo-id}"
```

**Issue title convention:** `[Sprint N] {Verb} {Noun}`
(e.g., `[Sprint 1] Add BlogPost entity and repository`)

**Mandatory format — every issue must satisfy both:**

| Field | Requirement |
|-------|-------------|
| Title | Must start with `[Sprint N]` prefix |
| Milestone | Must be set to `Sprint N: {Theme}` before any branch is created |

> **Hard rule:** An issue without a milestone or without the `[Sprint N]` prefix in its
> title is incomplete. No branch or code may reference it until both fields are set.

After creating each issue, **Aragorn immediately triages** it:
- Replace `squad` label with `squad:{member}` (e.g., `squad:sam`)
- This triggers normal issue routing for that member

---

## Step 4 — Aragorn: Add Issues to Project Board

Add each new issue to the **MyBlog** GitHub Project:

```bash
# Find the project number
gh project list --owner mpaulosky

# Add issue to project (use the URL returned by `gh issue create`)
gh project item-add 4 --owner mpaulosky --url {issue-url}
```

New items land in **Backlog** automatically. Move to **In Sprint** when the sprint begins:

```bash
# Update item status to "In Sprint"
# (requires field ID and option ID — retrieve once and store)
gh project item-edit \
  --id {ITEM_ID} \
  --field-id {STATUS_FIELD_ID} \
  --project-id {PROJECT_ID} \
  --single-select-option-id {IN_SPRINT_OPTION_ID}
```

---

## Step 5 — Boromir: Create Sprint Branches and Worktrees

For each sprint, create a long-lived sprint branch and a local worktree:

```bash
# Always start from latest dev
git checkout dev && git pull origin dev

# Create sprint branch
git checkout -b sprint/{N}-{slug}
git push -u origin sprint/{N}-{slug}
git checkout dev

# Create worktree (one directory up from repo root)
git worktree add ../MyBlog-sprint-{N} sprint/{N}-{slug}
```

**Example — Sprint 1 "Foundation":**

```bash
git checkout dev && git pull origin dev
git checkout -b sprint/1-foundation
git push -u origin sprint/1-foundation
git checkout dev
git worktree add ../MyBlog-sprint-1 sprint/1-foundation
```

**Naming:**

| Item | Convention |
|------|-----------|
| Sprint branch | `sprint/{N}-{slug}` |
| Worktree directory | `../MyBlog-sprint-{N}/` |

---

## Step 6 — Working a Sprint

Squad members working Sprint N:

```bash
# Enter the sprint worktree
cd ../MyBlog-sprint-{N}

# Create a feature branch FROM the sprint branch
git checkout -b squad/{issue}-{slug}

# ... do work, commit, push ...

# Open PR targeting the SPRINT branch, NOT dev
gh pr create \
  --base sprint/{N}-{slug} \
  --title "feat(scope): description (#issue)" \
  --body "Closes #{issue-number}" \
  --assignee @me
```

The standard **PR merge process** (`pr-merge-process.md`) applies normally,
but the base branch is `sprint/{N}-{slug}` instead of `dev`.

Move the project board item to **In Review** when the PR is open.
Move to **Done** after it merges into the sprint branch.

---

## Step 7 — Sprint Close

When all issues in the sprint milestone are resolved:

### 7a — Ralph verifies 100% completion

```bash
gh api repos/mpaulosky/MyBlog/milestones/{milestone_number} \
  | jq '{title, open_issues, closed_issues}'
# open_issues must be 0 before proceeding
```

### 7b — Aragorn opens Sprint → dev PR

```bash
gh pr create \
  --base dev \
  --head sprint/{N}-{slug} \
  --title "sprint({N}): merge sprint {N} — {theme} (#milestone)" \
  --body "Closes milestone: Sprint {N}: {Theme}

## Sprint Summary
- {list of key changes}

## Issues Closed
- Closes #{issue-1}
- Closes #{issue-2}

## Checklist
- [ ] All sprint issues closed
- [ ] CI green
- [ ] Milestone at 100%"
```

Full PR review process applies (pr-merge-process.md). Squash merge into `dev`.

### 7c — Close the milestone

```bash
gh api -X PATCH repos/mpaulosky/MyBlog/milestones/{milestone_number} \
  -f state="closed"
```

### 7d — Boromir removes the worktree

```bash
git worktree remove ../MyBlog-sprint-{N}
git push origin --delete sprint/{N}-{slug}
git branch -d sprint/{N}-{slug}
```

---

## Step 8 — Release Gate (all sprints done)

When **all** sprint milestones are closed:

1. **Aragorn** reviews the project board — all issues in `Done` column
2. **Aragorn** initiates the release playbook: `.squad/playbooks/release-myblog.md`
3. **Release PR:** `dev` → `main` (standard release flow)
4. After merge, move all project board items to `Released`

---

## Project Board Reference

| Column | Meaning |
|--------|---------|
| `Backlog` | Created, not yet in an active sprint |
| `In Sprint` | Assigned to the current active sprint |
| `In Review` | PR open, CI running |
| `Done` | Merged into sprint branch or dev |
| `Released` | Merged into main |

---

## Conventions Summary

| Item | Convention |
|------|-----------|
| Milestone name | `Sprint N: {Theme}` |
| Issue title | `[Sprint N] {Verb} {Noun}` |
| Sprint branch | `sprint/{N}-{slug}` |
| Worktree directory | `../MyBlog-sprint-{N}/` |
| Feature branch (in sprint) | `squad/{issue}-{slug}` (targets sprint branch) |
| Sprint PR base | `sprint/{N}-{slug}` |
| Sprint close PR base | `dev` |
| Release PR base | `main` |
| GH Project name | `MyBlog` |

---

## Hard Gate — No Code Before Issue

> **This rule is absolute and has no exceptions.**

Before any agent writes, modifies, or commits code, a GitHub issue **must** exist for the work. This gate applies to every work request regardless of how it arrives — `[[PLAN]]`, direct user instruction, or agent initiative.

**Enforcement sequence (runs before Step 1):**

```
1. Does a GitHub issue exist for this work?
   YES → does the issue have:
         a) a milestone set to "Sprint N: {Theme}"?
         b) a title starting with "[Sprint N]"?
         If either is missing → fix it now before touching any file.
         Then proceed to Step 1.
   NO  → CREATE the issue now before touching any file
         → Title MUST start with "[Sprint N]"
         → Milestone MUST be set to "Sprint N: {Theme}"
         → Add to Project #4
         → Create squad/{issue}-{slug} branch
         → THEN and only then begin writing code
```

If you skip this gate and write code without an issue, or create an issue without the
sprint prefix and milestone, you have violated the squad's process. The work must be
stashed, the issue corrected, a proper branch checked out, and the stash re-applied
before committing. This costs time — follow the gate.

---

## Anti-Patterns

- ❌ **Writing any code before a GitHub issue exists** — always create the issue first
- ❌ **Creating an issue without setting its milestone** — every issue must be assigned to `Sprint N: {Theme}` before any branch is created
- ❌ **Creating an issue whose title does not start with `[Sprint N]`** — the sprint prefix is mandatory; bare titles like "Fix login bug" are invalid
- ❌ **Implementing a `[[PLAN]]` request without first running the sprint planning ceremony** — plan → issue → branch → code
- ❌ **Opening `squad/{issue}` PRs directly to `dev`** during an active sprint
- ❌ **Skipping worktree** — always work in `../MyBlog-sprint-{N}/` for isolation
- ❌ **Closing milestone before all issues resolve** — Ralph confirms 0 open issues
- ❌ **Releasing from a sprint branch** — only `dev` → `main` releases
- ❌ **Starting sprint N+1 before sprint N PR merges to dev** — sequential sprint close
- ❌ **Deleting worktree before sprint PR merges** — leads to lost work

---

## Related Documents

- **Ceremony:** `.squad/ceremonies.md` (Sprint Planning)
- **Skill:** `.squad/skills/sprint-planning/SKILL.md`
- **PR merge process:** `.squad/playbooks/pr-merge-process.md`
- **Pre-push gate:** `.squad/playbooks/pre-push-process.md`
- **Release process:** `.squad/playbooks/release-myblog.md`
- **Routing:** `.squad/routing.md` (sprint planning trigger)
