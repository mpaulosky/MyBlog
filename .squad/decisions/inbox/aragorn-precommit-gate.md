# Decision: Pre-Commit Markdownlint Gate

**Date:** 2026-04-25
**Author:** Aragorn (Lead / Architect)
**PR:** #232
**Branch:** `squad/230-precommit-markdownlint-gate`

## Context

PR #229 fixed 3,243+ markdownlint violations across all `.md` files and added `.markdownlint.json` to the repo root. Without a commit-time gate, those violations could easily regress as contributors edit documentation.

## Decision

Add a pre-commit git hook (`.github/hooks/pre-commit`) that runs `markdownlint-cli2` on staged `.md` files only before each local commit.

## Rationale

- **Staged-only linting** is fast — no full-repo scan on every commit.
- **Graceful degradation** — warns but does not block if the binary is absent, preserving developer ergonomics for contributors who have not run `npm install`.
- **Consistent config** — reuses the existing `.markdownlint.json` so rules cannot diverge between the hook and CI.
- **Pattern consistency** — mirrors the pre-push hook pattern already established in `.github/hooks/pre-push` and `scripts/install-hooks.sh`.

## Implementation

- `.github/hooks/pre-commit` — the hook source (tracked in git)
- `scripts/install-hooks.sh` — updated to install both `pre-push` and `pre-commit` hooks
- `package.json` — added `markdownlint-cli2 ^0.17.2` as dev dependency

## Binary probe order

1. `markdownlint` (global CLI)
2. `./node_modules/.bin/markdownlint-cli2`
3. `./node_modules/.bin/markdownlint`
4. Not found → warn, exit 0 (graceful degrade)

## Trade-offs

- `package-lock.json` grows with the new dependency — acceptable for a DX tooling dep.
- Contributors must run `npm install` to get the linter; the hook warns them if they haven't.
