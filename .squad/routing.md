# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Architecture decisions, ADRs, PR gates, triage | Aragorn | Solution design, dependency rules, PR review, breaking changes |
| Domain model, backend services, data layer | Sam | BlogPost entity, repositories, EF Core, caching, Aspire wiring |
| Blazor UI, components, features, layout | Legolas | Feature slices, pages, components, Auth UI, NavMenu |
| Unit, Architecture & Integration tests | Gimli | xUnit, FluentAssertions, NSubstitute, NetArchTest, coverage |
| CI/CD, build pipeline, Aspire config, infra | Boromir | GitHub Actions workflows, AppHost resources, Docker |
| Code review gate, quality assurance | Gandalf | PR approval/rejection, coding standards, pattern enforcement |
| Auth0, security, secrets management | Frodo | Auth0 roles, Management API, token claims, secrets |
| Docs, README, ADRs, changelogs, summaries | Pippin | README.md, ARCHITECTURE.md, CONTRIBUTING.md, release notes |
| Research, spikes, POCs, library evaluation | Bilbo | Technology comparisons, proof-of-concept prototypes |
| Code review | Gandalf | Review PRs, check quality, suggest improvements |
| Testing | Gimli | Write tests, find edge cases, verify fixes |
| Scope & priorities | Aragorn | What to build next, trade-offs, decisions |
| Session logging | Scribe | Automatic — never needs routing |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, assign `squad:{member}` label | Lead |
| `squad:{name}` | Pick up issue and complete the work | Named member |

### How Issue Assignment Works

1. When a GitHub issue gets the `squad` label, the **Lead** triages it — analyzing content, assigning the right `squad:{member}` label, and commenting with triage notes.
2. When a `squad:{member}` label is applied, that member picks up the issue in their next session.
3. Members can reassign by removing their label and adding another member's label.
4. The `squad` label is the "inbox" — untriaged issues waiting for Lead review.

## Skills

When routing work in these domains, inject the listed asset into the agent's
spawn prompt:
`Relevant asset: {path} — read before starting.`

| Domain | Asset | When to Inject |
|--------|-------|----------------|
| Blazor Tailwind theming, dark/light mode, FOUC, localStorage, color themes | `.squad/skills/blazor-tailwind-theme-persistence/SKILL.md` | Any Legolas task touching App.razor, NavMenu, MainLayout, theme toggle, or `tailwind-color-theme` storage key |
| Auth0 Management API, M2M, role operations | `.squad/skills/auth0-management-api/SKILL.md` | Any Frodo or Legolas task touching UserManagementHandler, role operations, Management API integration, or Auth0 configuration changes. Owner: Frodo (Tech Writer). |
| Auth0 security, secrets, authorization | `.squad/skills/auth0-management-security/SKILL.md` | Any security audit, secrets review, or auth configuration change. All squad members reference this for authorization boundary and secrets management rules. Owner: Frodo (Tech Writer). |
| MongoDB DBA patterns, runtime wiring, indexing | `.squad/skills/mongodb-dba-patterns/SKILL.md` | Any Sam, Gimli, Boromir, or Frodo task touching Mongo wiring, mapping, indexing, backups, upgrades, or shared environment hardening. Owner: Sam (Backend). Audience: Gimli (verification), Boromir (environment), Frodo (secrets/TLS). |
| MongoDB filter patterns, list queries, caching | `.squad/skills/mongodb-filter-pattern/SKILL.md` | Any Sam or Gimli task touching query contracts, cache-key changes, list filtering, repository standardization, or handler-level caching. Owner: Sam (Backend). Supporting: Gimli (Tester). |
| Mongo-backed integration tests | `.squad/skills/testcontainers-shared-fixture/SKILL.md` | Any Gimli or Sam task touching `tests/Integration.Tests/`, `MongoDbFixture`, collection definitions, or new repository/handler integration coverage against MongoDB. Owner: Gimli (Tester). |
| Running-browser UI verification | `.squad/skills/webapp-testing/SKILL.md` | Any Gimli or Legolas task that already has bUnit coverage but still needs runtime verification of JS interop, Auth0 redirects, or AppHost smoke behavior. Do **not** inject this for ordinary unit/bUnit work or to create a new browser-test project. Owner: Gimli (Tester). |
| Push-capable squad work | `.squad/skills/pre-push-test-gate/SKILL.md` + `.squad/playbooks/pre-push-process.md` | Any task expected to end in `git push`, branch handoff, or local gate validation. This is the default for normal `squad/{issue}-{slug}` delivery after Sprint 1.1. |
| Build/test gate failures | `.squad/skills/build-repair/SKILL.md` + `.squad/skills/pre-push-test-gate/SKILL.md` | Any task blocked by Release build failures, warning cleanup, failing tests, or a rejected pre-push Gates 2–4 run. Aragorn owns this route and can delegate the repair. |
| PR review, approval, merge, and post-merge cleanup | `.squad/playbooks/pr-merge-process.md` | Any Aragorn-led PR gate once CI is green, including Copilot-review read, parallel reviewer fan-out, CHANGES_REQUESTED lockout, squash merge, and cleanup. |
| Resumed work on an existing `squad/*` branch | `.squad/skills/merged-pr-guard/SKILL.md` | Any agent about to `git commit` on a branch with prior PR activity or an uncertain session state. Check for an already-merged PR before committing. |
| Imported non-fit skills | `.squad/skills/building-protection/SKILL.md` | Do **not** inject for MyBlog work. This skill is Minecraft-specific and stays quarantined until Milestone 3 disposition work decides whether to adapt or delete it. |
| NuGet/Azure/Microsoft API reference during CI/CD | `.squad/skills/microsoft-code-reference/SKILL.md` | Any Boromir task verifying NuGet package signatures, Azure SDK methods, GitHub Actions patterns, or .NET Framework API compatibility. Owner: Boromir (DevOps). Marked for Sprint 2 scope rewrite (DevOps/NuGet/GitHub Actions focus). |

## Workflow Guardrails

After Sprint 1.1, these process assets are part of normal squad flow:

1. **Before any push-ready handoff**, route through the pre-push gate skill and
   pre-push playbook so agents respect the live MyBlog hook: `squad/{issue}-{slug}`
   branch naming, Release build, `Architecture.Tests`, `Unit.Tests`, and
   `Integration.Tests`.
2. **When build or test health is red**, route through build repair first. Do not
   treat a broken branch as normal feature work.
3. **When PR work starts**, Aragorn and any spawned reviewers use the PR merge
   playbook as the governing checklist.
4. **When a session resumes on an older squad branch**, apply the merged-PR guard
   before committing so work does not strand on a merged branch.
5. **Do not normalize quarantined imports.** If an asset does not fit MyBlog yet,
   keep it out of normal routing until a later disposition decision is made.

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If a feature is being built, spawn the tester to write test cases from requirements simultaneously.
7. **Issue-labeled work** — when a `squad:{member}` label is applied to an issue, route to that member. The Lead handles all `squad` (base label) triage.
