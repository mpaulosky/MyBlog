# Ceremonies

> Team meetings that happen before or after work. Each squad configures their own.

## Design Review

| Field | Value |
|-------|-------|
| **Trigger** | auto |
| **When** | before |
| **Condition** | multi-agent task involving 2+ agents modifying shared systems |
| **Facilitator** | lead |
| **Participants** | all-relevant |
| **Time budget** | focused |
| **Enabled** | ✅ yes |

**Agenda:**
1. Review the task and requirements
2. Agree on interfaces and contracts between components
3. Identify risks and edge cases
4. Assign action items

---

## Sprint Planning

| Field | Value |
|-------|-------|
| **Trigger** | auto |
| **When** | on plan creation or material update |
| **Condition** | any `plan.md` created or materially updated |
| **Facilitator** | Ralph (decompose) + Aragorn (GH artifacts) |
| **Participants** | Ralph, Aragorn, Boromir |
| **Time budget** | focused |
| **Enabled** | ✅ yes |

**Agenda:**
1. Ralph reviews `plan.md` and SQL todos, groups into logical sprints (3–6 issues each)
2. Aragorn creates one GitHub milestone per sprint (`Sprint N: {Theme}`)
3. Aragorn creates GitHub issues per todo, assigned to milestones, triaged with `squad:{member}` label
4. Aragorn adds all issues to the **MyBlog** GitHub Project board (Backlog column)
5. Boromir creates `sprint/{N}-{slug}` branch and `../MyBlog-sprint-{N}/` worktree per sprint

**See:** `.squad/playbooks/sprint-planning.md`

---

## Retrospective

| Field | Value |
|-------|-------|
| **Trigger** | auto |
| **When** | after |
| **Condition** | build failure, test failure, or reviewer rejection |
| **Facilitator** | lead |
| **Participants** | all-involved |
| **Time budget** | focused |
| **Enabled** | ✅ yes |

**Agenda:**
1. What happened? (facts only)
2. Root cause analysis
3. What should change?
4. Action items for next iteration
