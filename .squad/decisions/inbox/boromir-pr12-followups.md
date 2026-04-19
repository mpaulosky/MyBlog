# PR #12 Follow-ups: Pre-Push Gate References

**Date:** 2026-04-19  
**Author:** Boromir (DevOps Engineer)  
**Status:** ✅ Implemented  
**PR:** #12

## Decision

The pre-push skill should point contributors to `docs/CONTRIBUTING.md` as the
authoritative setup and usage guide instead of referencing a non-existent
`.squad/playbooks/pre-push-process.md` playbook.

## Rationale

- `docs/CONTRIBUTING.md` already documents hook installation and the five
  pre-push gates.
- Reusing the canonical contributor guide avoids duplicating operational
  instructions in a second document.
- Removing the dead `.squad/playbooks/...` reference keeps the skill accurate
  for new contributors and agents.
