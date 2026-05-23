# Copilot Coding Agent Instructions

This repository uses **Squad** for orchestration and follows project-level .NET conventions.

## Squad Workflow (Required)

Before starting work on any issue:

1. Read `.squad/team.md` for the roster, roles, and capability profile.
2. Read `.squad/routing.md` for routing rules.
3. If the issue has a `squad:{member}` label, read `.squad/agents/{member}/charter.md` and work in that role's style.

### Capability Self-Check

Check `.squad/team.md` under **Coding Agent → Capabilities**:

- **🟢 Good fit** — proceed autonomously.
- **🟡 Needs review** — proceed, and add reviewer guidance in the PR body.
- **🔴 Not suitable** — do not implement; comment on the issue:

  ```
  🤖 This issue doesn't match my capability profile (reason: {why}). Suggesting reassignment to a squad member.
  ```

### Branch Naming

Use:

```
squad/{issue-number}-{kebab-case-slug}
```

Example: `squad/42-fix-login-validation`

### Pull Request Requirements

- Reference the issue: `Closes #{issue-number}`
- If labeled `squad:{member}`, include: `Working as {member} ({role})`
- For 🟡 tasks, include: `⚠️ This task was flagged as "needs review" — please have a squad member review before merging.`
- Follow decisions in `.squad/decisions.md`

### Team Decision Drop Box

If your work introduces a team-relevant decision, write it to:

```
.squad/decisions/inbox/copilot-{brief-slug}.md
```

Scribe will merge inbox entries into `.squad/decisions.md`.

## Project Engineering Conventions

### Platform

- Target .NET 10 (`net10.0`) and latest stable patch SDK/runtime
- Use C# 14 language features where they improve clarity

### C# and Solution Style

- Respect `.editorconfig` and existing repository formatting
- Use explicit types when helpful; use `var` when type is obvious
- Prefer null checks with `is null` / `is not null`
- Use file-scoped namespaces, nullable reference types, and pattern matching
- Keep naming conventions consistent (`I*` interfaces, `Async` suffix, `_privateField`)

### Architecture

- Keep dependency injection and strongly typed options/configuration
- Prefer async/await end-to-end in app code and tests
- Maintain vertical-slice/CQRS patterns where they already exist
- Keep package versions centralized in `Directory.Packages.props`

### Security and Middleware

- Keep HTTPS, authentication, authorization, antiforgery, and secure headers enabled
- Preserve global exception handling and request logging patterns

### Blazor and Web UI

- Maintain existing component lifecycle/state-management patterns
- Keep interactive rendering strategy and shared component conventions
- Preserve error boundary patterns and existing UI composition style

### Data and Testing

- Keep MongoDB + EF Core integration patterns used in this repo
- Prefer async data access
- Keep unit, integration, architecture, and UI tests current with behavior changes

### Documentation

- Keep README/CONTRIBUTING and inline documentation aligned with behavioral changes
- Use OpenAPI + Scalar conventions for HTTP API documentation where applicable
