# Squad Commands — Quick Reference

A practical reference for working with the Squad AI team in GitHub Copilot.
All commands are typed as plain chat messages in the Copilot CLI terminal.

---

## Session Commands

| Command | What it does |
|---------|-------------|
| `squad start Sprint N` | Kicks off a sprint — fans the team out to begin work on all open sprint issues in parallel. |
| `fan squad begin work` | Fans out to all relevant squad members to start work simultaneously. |
| `clean up` | Deletes merged local and remote `squad/*` branches, prunes stale remotes, and syncs `main` and `dev`. |

---

## Invoking Team Members

Call a member by name to assign them a task. Combine with a description for a directed request.

| Command | What it does |
|---------|-------------|
| `{Member}` | Wakes the member and routes them based on context (e.g. open PR, current branch). |
| `{Member} go` | Tells a waiting member to proceed with their current task. |
| `{Member} {task description}` | Assigns a specific task to that member (e.g. `Sam add Redis caching to the BlogPost handler`). |

**Examples:**

```text
Ralph, go
Boromir review the CI workflow
Gimli write tests for the new ICacheService
Bilbo write a release blog post for v1.2.0
```

---

## PR Review Commands

| Command | What it does |
|---------|-------------|
| `Review PR #{N}` | Shorthand — engages Aragorn as lead reviewer. Equivalent to `Aragorn review PR #{N}`. |
| `{Member} review PR #{N}` | Member follows Critical Rule #7: waits for CI → reads Copilot review → posts verdict → merges if approved. |
| `{Member} wait for the gh review on PR #{N}` | Member watches CI and the GitHub Copilot automated review before posting their own verdict. |

> **Aragorn's review sequence (Critical Rules #7 & #8):**
> CI pass → read Copilot review (fix bugs/security; style is discretionary) → parallel domain review → fix cycle if rejected → approve → squash merge → sync local.
> Before any push: full test suite must pass locally — CI is never the first place failures are found.

**Examples:**

```text
Review PR #131
Aragorn review PR #128
Aragorn wait for the gh review on PR #126 the review
```

---

## Fan-Out Commands

| Command | What it does |
|---------|-------------|
| `squad fan out {description}` | Spawns all relevant members in parallel to tackle a broad task (e.g. docs update, multi-area refactor). |
| `Team, {task}` | Same as fan-out — spawns all members whose domain touches the task. |

**Example:**

```text
squad fan out we need to update all our documentation README.md, Add missing Blogs,
Add Release summaries, Update the docs/index.html with the new README.md info
along with Release notes and a list of linked Blogs.
```

---

## Sprint & Release Commands

| Command | What it does |
|---------|-------------|
| `Is Sprint N a release candidate?` | Asks Ralph to evaluate whether all sprint issues are closed, CI is green, and the sprint is ready to ship. |
| `Ralph` | Invokes Ralph (meta agent) for sprint planning, milestone management, and squad maintenance. |

**Example flow:**

```text
Is Sprint 5 a release candidate?
→ Ralph confirms yes
→ PR from dev → main opens
→ Aragorn review PR #{N}
→ Merged and tagged
```

---

## Team Members at a Glance

| Member | Role | Owns |
|--------|------|------|
| **Aragorn** | Lead / Architect | PR review gates, ADRs, architecture decisions, triage |
| **Sam** | Backend / .NET | Domain model, handlers, repositories, caching, Aspire wiring |
| **Legolas** | Frontend / Blazor | Blazor components, pages, UI, theming |
| **Gimli** | Tester | Unit tests, architecture tests, integration tests, coverage |
| **Boromir** | DevOps / Infra | GitHub Actions CI/CD, Aspire config, Docker, workflows |
| **Gandalf** | Security / Auth | Auth0 roles, secrets, vulnerability review |
| **Frodo** | Tech Writer | XML docs, inline comments, README (focused updates) |
| **Pippin** | Docs | README, ARCHITECTURE.md, ADRs, changelogs, release notes |
| **Bilbo** | Tech Blogger | Blog posts, release posts, `docs/blog/`, GitHub Pages |
| **Ralph** | Meta | Sprint planning, milestone creation, squad maintenance |
| **Scribe** | Logger | Session checkpoints and logs (always runs automatically) |

---

## Workflow Guardrails (Quick Reference)

These rules are enforced automatically — know them to avoid blocked work:

- **Issue required before code** — Every piece of work needs a GitHub issue with `[Sprint N]` title prefix and a sprint milestone.
- **Branch naming** — Feature branches must follow `squad/{issue-number}-{kebab-slug}`.
- **No `--no-verify` pushes** — Pre-push hook runs build + tests + coverage. Bypassing is prohibited.
- **PR must close an issue** — All PRs need `Closes #N` in the body.
- **Unanimous approval to merge** — All required reviewers (Aragorn + domain specialists) must approve.
- **Squash merge only** — Use `--squash` to keep history clean.

---

## Common Sequences

### Start a sprint

```text
squad start Sprint N
fan squad begin work
```

### Ship a feature PR

```text
Review PR #{N}                   ← CI check + Copilot review + verdict + merge
clean up                         ← remove merged branches, sync repos
```

### Release a sprint to main

```text
Is Sprint N a release candidate?
→ yes
→ PR opens dev → main
Review PR #{N}
→ approved and merged
```

### Manual board fix (Done → Released)

```text
Go to Actions → Squad Mark Released → Run workflow
Provide either:
- release_pr_number = merged release PR to main
- tag_name = published release tag (for example v1.7.0)
```

> **How "shipped items" are determined:**
> The workflow scans three sources for `Closes #N` / `Fixes #N` references:
>
> 1. **Commit messages** in the release PR (new since the previous release)
> 2. **Squad PR bodies** — bodies of merged non-release PRs associated with those commits
> 3. **The release/hotfix PR body itself** — for hotfix releases the `Closes #N` reference typically lives only here
>
> Only issues whose project-board status is currently **Done** are promoted to **Released**.
> Issues in any other column (In Sprint, In Review, Released, etc.) are skipped.

---

> **Related files:** `.squad/routing.md` · `.squad/team.md` · `.squad/playbooks/` · `.squad/ceremonies.md`
