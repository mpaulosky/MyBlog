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

When routing work in these domains, inject the listed skill into the agent's spawn prompt:
`Relevant skill: .squad/skills/{name}/SKILL.md — read before starting.`

| Domain | Skill | When to Inject |
|--------|-------|----------------|
| Blazor Tailwind theming, dark/light mode, FOUC, localStorage, color themes | `blazor-tailwind-theme-persistence` | Any Legolas task touching App.razor, NavMenu, MainLayout, theme toggle, or `tailwind-color-theme` storage key |

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If a feature is being built, spawn the tester to write test cases from requirements simultaneously.
7. **Issue-labeled work** — when a `squad:{member}` label is applied to an issue, route to that member. The Lead handles all `squad` (base label) triage.
