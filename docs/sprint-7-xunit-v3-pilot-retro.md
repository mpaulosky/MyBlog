---
post_title: "Sprint 7 xUnit v3 Pilot — Retrospective Report"
author1: "Aragorn (Lead Developer)"
post_slug: "sprint-7-xunit-v3-pilot-retro"
microsoft_alias: ""
featured_image: ""
categories: ["Engineering", "Testing"]
tags: ["xunit", "xunit-v3", "testing", "pilot", "retrospective", "sprint-7"]
ai_note: "Assisted"
summary: "Retrospective report for the Sprint 7 xUnit v3 pilot migration of Domain.Tests. Covers what worked, what broke, process improvements, and the decision on full migration for Sprints 8–10."
post_date: "2025-07-01"
---

## Context

Sprint 7 piloted the migration of `tests/Domain.Tests` from **xUnit v2** to **xUnit v3** as a
controlled experiment before committing to a full migration across all test projects. The pilot
scope was deliberately narrow — Domain.Tests only — to isolate risk and establish a repeatable
migration playbook.

**Sprint Theme:** xUnit v3 Pilot  
**Sprint Branch:** `sprint/7-xunit-v3-pilot`  
**Issues in scope:** #162 – #166 (migration), #167 (this retro)  
**Related decisions:** See `.squad/decisions.md` § xUnit v3 Migration

---

## Pilot Scope

| Test Project | Sprint 7 Action | Owner |
|---|---|---|
| `tests/Domain.Tests` | ✅ Migrated to xUnit v3 | Gimli |
| `tests/Unit.Tests` | 🔜 Planned (Sprint 8) | Gimli |
| `tests/Architecture.Tests` | 🔜 Planned (Sprint 9) | Gimli |
| `tests/Blazor.Tests` (bUnit) | 🔜 Planned (Sprint 10) | Gimli |

**Why Domain.Tests first?**

- Fewest external dependencies (no Blazor, no integration containers)
- Pure C# / CQRS handler tests — easiest to isolate breaking changes
- Provides a migration template for the other three projects

---

## Metrics to Track

> **📋 To be filled in after sprint work (#162–#166) completes.**

### Build Metrics

| Metric | Baseline (v2) | Sprint 7 (v3) | Delta |
|---|---|---|---|
| Release build time | _TBD_ | _TBD_ | _TBD_ |
| Build warnings | _TBD_ | _TBD_ | _TBD_ |
| Compiler errors on upgrade | N/A | _TBD_ | — |

### Test Metrics — Domain.Tests

| Metric | Baseline (v2) | Sprint 7 (v3) | Delta |
|---|---|---|---|
| Total tests | _TBD_ | _TBD_ | _TBD_ |
| Pass rate | _TBD_ | _TBD_ | _TBD_ |
| Execution time (s) | _TBD_ | _TBD_ | _TBD_ |
| Line coverage % | _TBD_ | _TBD_ | _TBD_ |
| Branch coverage % | _TBD_ | _TBD_ | _TBD_ |

### CI Metrics

| Metric | Sprint 6 (v2) | Sprint 7 (v3) | Delta |
|---|---|---|---|
| CI feedback time (min) | _TBD_ | _TBD_ | _TBD_ |
| Flaky test count | _TBD_ | _TBD_ | _TBD_ |

---

## Success Criteria

The pilot passes if **all** of the following are true at sprint end:

- [ ] `tests/Domain.Tests` compiles against xUnit v3 NuGet packages
- [ ] All pre-existing Domain.Tests tests pass without skips or workarounds
- [ ] Line coverage ≥ 80% (no regression from Sprint 6 baseline)
- [ ] Release build (`dotnet build -c Release`) exits 0 with 0 errors
- [ ] CI workflow `squad-test.yml` passes end-to-end on the sprint branch
- [ ] No breaking change propagates to `Unit.Tests`, `Architecture.Tests`, or `Blazor.Tests`
- [ ] Migration playbook documented (see Phase 2 of this doc)

---

## Results

> **📋 To be filled in by Aragorn after sprint work completes (Phase 2).**

### What Worked

- _TBD — populate after sprint close_

### Issues Encountered

- _TBD — see tracking doc `.squad/decisions/inbox/aragorn-sprint7-xunit-pilot-tracking.md`_

### Breaking Changes / Workarounds

- _TBD_

---

## Lessons Learned

> **📋 To be filled in by Aragorn after sprint close.**

### What Went Well

- _TBD_

### What Could Be Better

- _TBD_

### Process Improvements for Sprints 8–10

- _TBD_

---

## Decision: Full Migration?

> **📋 Pending sprint close. Options:**

| Option | Description |
|---|---|
| ✅ **Proceed** | xUnit v3 works well — execute phased rollout Sprints 8–10 |
| ⚠️ **Iterate** | Issues found — fix playbook, re-pilot before expanding |
| ❌ **Abort** | Blocking problems — stay on xUnit v2, close migration issues |

**Recommendation:** _TBD_  
**Decision date:** _TBD_  
**Decided by:** Aragorn (Lead Developer)

---

## Next Steps

> **📋 If decision = Proceed:**

| Sprint | Scope | Owner |
|---|---|---|
| Sprint 8 | Migrate `tests/Unit.Tests` | Gimli |
| Sprint 9 | Migrate `tests/Architecture.Tests` | Gimli |
| Sprint 10 | Migrate `tests/Blazor.Tests` (bUnit) | Gimli + Legolas |

> **📋 If decision = Iterate or Abort:**

- Document blockers in `.squad/decisions.md`
- Create follow-up issues for unresolved items
- Notify Gimli, Boromir, Pippin of scope change

---

## Appendix: Migration Playbook (Draft)

> Populated during Phase 2 after Domain.Tests migration is complete.
> This section becomes the reusable template for Unit.Tests, Architecture.Tests, Blazor.Tests.

### Package Changes

```xml
<!-- xUnit v2 (remove) -->
<PackageReference Include="xunit" Version="2.*" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.*" />

<!-- xUnit v3 (add) -->
<PackageReference Include="xunit.v3" Version="..." />
<PackageReference Include="xunit.runner.visualstudio" Version="3.*" />
```

### API Breaking Changes

_TBD — document any `[Fact]`, `[Theory]`, `IClassFixture`, `IAsyncLifetime` changes here._

### Assertion Library Compatibility

_TBD — confirm FluentAssertions, NSubstitute compatibility with xUnit v3._

### CI Runner Compatibility

_TBD — confirm `dotnet test` and coverage collection work with xUnit v3 runner._
