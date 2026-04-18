# Decision: CI Workflow Conventions — global.json SDK and Version Stamping

**Date:** 2026-04-18
**Author:** Boromir (DevOps & CI/CD Engineer)
**PR:** #9 — squad/cicd-phase3-4

## Context

PR #9 introduced GitVersion integration and parallel test workflows. Copilot review flagged five issues that established important conventions for all future workflow authoring.

## Decisions

### 1. Always use `global-json-file: global.json` in setup-dotnet

When `global.json` is present, use:
```yaml
- uses: actions/setup-dotnet@v4
  with:
    global-json-file: global.json
```

**Never use** `dotnet-version` + `dotnet-quality: 'preview'` when `global.json` exists. The two conflict when `allowPrerelease: false` is set.

### 2. Use `nuGetVersion` for `/p:Version` in dotnet build

```yaml
dotnet build ... /p:Version=${{ steps.gitversion.outputs.nuGetVersion }} /p:InformationalVersion=${{ steps.gitversion.outputs.informationalVersion }}
```

`assemblySemVer` strips prerelease labels. `nuGetVersion` preserves them. Always stamp `InformationalVersion` for full git metadata.

### 3. squad-test.yml is PR-only

`squad-test.yml` (parallel tests) must only trigger on `pull_request`. Remove all `push` triggers from it. The `ci.yml` handles push events including sequential tests. Separate responsibilities prevent duplicate CI runs.

### 4. `continue-on-error: true` must be surgical

Only use `continue-on-error: true` on steps that are genuinely optional:
- ✅ PR comment posting (may lack permission in forks)
- ✅ Notification/badge steps
- ❌ Artifact download (if download fails, report is wrong)
- ❌ Coverage generation (if generation fails, data is missing)

### 5. Comments must not contradict code

A comment like "// Get the default branch name (main, master, etc.)" next to `const baseBranch = 'dev'` is actively harmful. Either remove the comment or update it to say "// Base branch for squad PRs".

## Impact

- Affects all future workflow files in `.github/workflows/`
- Any workflow touching .NET setup must use `global-json-file`
- Any workflow using GitVersion must use `nuGetVersion` for version stamping
