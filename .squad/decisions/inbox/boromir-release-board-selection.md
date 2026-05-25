## 2026-05-25: Release board selection uses release-PR commit scope

**By:** Boromir
**What:** Project board release promotion now compares the current merged
release PR's commit set to the previous merged release PR's commit set, then
moves only issue cards linked to newly shipped commits and still in the Done
status by stable Project v2 field and option IDs.

**Why:** The previous Done → Released automation promoted unrelated cards
because it selected the entire Done column and trusted release PR body refs
like recovery/meta issue links. Using the release commit delta keeps normal
`dev` → `main`, recovery release branches, and manual tag-driven reruns
aligned to what actually shipped without relying on rename-sensitive board
matching or merge timestamps alone.
