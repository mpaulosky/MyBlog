---
post_title: "Sprint 8 Retrospective — xUnit v3 Architecture.Tests Pilot"
author1: "Aragorn (Lead Developer)"
post_slug: "sprint-8-xunit-v3-pilot-retro"
microsoft_alias: ""
featured_image: ""
categories: ["Engineering", "Testing"]
tags: ["xunit", "xunit-v3", "testing", "architecture-tests", "retrospective", "sprint-8"]
ai_note: "Assisted"
summary: "Retrospective report for the Sprint 8 xUnit v3 pilot migration of Architecture.Tests. Covers the wave-based delivery model, backward compatibility of NetArchTest attributes, team dependency chain effectiveness, and recommendations for completing the xUnit v3 rollout."
post_date: "2025-07-24"
---

## Sprint Overview

**Sprint Theme:** xUnit v3 Architecture.Tests Pilot (extending Sprint 7 Domain.Tests pilot)
**Sprint Branch:** `sprint/8-xunit-v3-pilot`
**Sprint Goal:** Extend the xUnit v3 rollout from Domain.Tests (Sprint 7 pilot) to Architecture.Tests
**Sprint Issues:** #176 – #181

**Team:**

| Member | Role | Sprint 8 Scope |
|---|---|---|
| Boromir | DevOps Engineer | Waves 1a & 1b — Package setup and CI validation |
| Gimli | Tester | Wave 2 — Architecture.Tests code migration |
| Pippin | Docs | Wave 3a — ADR documentation |
| Aragorn | Lead Developer | Wave 3b — Sprint retrospective (this document) |

---

## Sprint Delivery Summary

| Issue | Wave | Owner | Status | Description |
|---|---|---|---|---|
| #176 | 1a | Boromir | ✅ Done | Add xUnit v3 packages to Architecture.Tests |
| #177 | 1b | Boromir | ✅ Done | CI validation for xUnit v3 Architecture.Tests |
| #178 | 2a | Gimli | ✅ Done | Migrate Architecture.Tests to xUnit v3 |
| #179 | 2b | Gimli | ✅ Done | Validate — no test failures after migration |
| #180 | 3a | Pippin | 🔄 In Progress | ADR documentation |
| #181 | 3b | Aragorn | 🔄 In Progress | Sprint retrospective (this document) |

**Total PRs:** 6 across Waves 1–3
**Test Results:** 11 Architecture tests passing (72ms), 42 Domain tests passing
**CI Status:** All green — `squad-preview.yml` passes both test projects

---

## What Went Well

### Wave 1 merged with zero rework

Both Boromir's PRs (#176/#182 and #177/#183) merged cleanly without any
rework iterations. The package strategy — referencing `xunit.v3` as the single
meta-package rather than individual sub-packages — matched the Domain.Tests Sprint 7
pattern exactly, removing all guesswork.

### CI passed immediately after package update

No CI failures, no unexpected compatibility issues. The `squad-preview.yml`
gate ran both `Architecture.Tests` and `Domain.Tests` and reported green in the first run.
This validated the Wave 1 → Wave 2 gating decision: Gimli could begin code migration
with confidence that the package foundation was solid.

### NetArchTest attributes require zero changes for xUnit v3

This was the single biggest time-saver in the sprint. Because Architecture.Tests
uses only `[Fact]` — no `[Theory]`, `IClassFixture`, or `IAsyncLifetime` — the
xUnit v2 → v3 migration involved **zero attribute changes**. The migration was
purely structural (AAA comment additions and variable extractions for clarity).

### Wave dependency chain worked as designed

The three-wave dependency model performed exactly as intended:

```
Wave 1 (Boromir): Packages + CI  →  Wave 2 (Gimli): Code migration  →  Wave 3 (Pippin + Aragorn): Docs + Retro
```

No team member was blocked by another wave's incomplete work. Boromir's Wave 1
completion was the prerequisite for Gimli's Wave 2, and Wave 2 completion
gated Wave 3. The fan-out within Wave 3 (Pippin and Aragorn working in parallel)
saved calendar time on documentation tasks.

### Domain.Tests CI gap resolved as a side effect

Boromir identified and fixed a pre-existing gap: `Domain.Tests` (Sprint 7 pilot)
was absent from `squad-preview.yml` despite being the reference implementation.
This was corrected in #177/#183 — now both test projects are CI-gated together,
which is the correct steady state.

### Architecture test run time is excellent

11 tests in 72ms with parallel execution enabled. This is a signal that the
`xunit.runner.json` parallelization setting (enabled in Wave 1) is working.
Stateless NetArchTest rules are inherently safe to parallelize.

---

## What Could Be Improved

### Sprint 7 retrospective was left as a draft/template

The `docs/sprint-7-xunit-v3-pilot-retro.md` file was created as a template
with `_TBD_` placeholders and never populated with actual results. Future
retrospectives should be authored as completed documents at sprint close,
not pre-populated templates left for later. This retrospective follows that
corrected approach.

### The planned migration order shifted between sprints

The Sprint 7 retrospective template listed the rollout order as:
- Sprint 8 → `Unit.Tests`
- Sprint 9 → `Architecture.Tests`

Sprint 8 actually targeted `Architecture.Tests` (not `Unit.Tests`). While the
change was pragmatic (Architecture.Tests has simpler migration surface), the
discrepancy means the decisions log needed updating. Future sprints should
document scope changes explicitly in `.squad/decisions.md` rather than silently
diverging from the previous retrospective's plan.

### No quantitative build time baseline

We don't have a before/after CI build time comparison (v2 → v3) because baseline
metrics were not recorded in Sprint 7. For Sprint 9 (if it targets additional
test projects), capture the `squad-preview.yml` run time before the migration
begins so we have a clean delta.

---

## Learnings & Patterns

### xUnit v3 package strategy (established pattern)

For any test project migrating to xUnit v3, follow the pattern established by
Boromir (Sprint 7/8):

```xml
<PackageReference Include="xunit.v3" />
<PackageReference Include="xunit.analyzers" />
<PackageReference Include="xunit.runner.visualstudio" />
<ItemGroup>
  <Using Include="Xunit" />
</ItemGroup>
```

Versions are centralized in `Directory.Packages.props` (3.2.2, 1.27.0, 3.1.1 respectively).
No individual `.csproj` version pins. See `.squad/decisions/inbox/boromir-xunit-v3-package-strategy.md`.

### Architecture test migration pattern (AAA comments)

NetArchTest's fluent builder API blurs the Arrange/Act boundary. Gimli
established two canonical forms:

**Full 3-part AAA (when assembly can be extracted):**

```csharp
// Arrange
var assembly = typeof(BlogPost).Assembly;

// Act
var result = Types.InAssembly(assembly)
    .That()...
    .GetResult();

// Assert
result.IsSuccessful.Should().BeTrue();
```

**Combined Arrange/Act (when assembly is a static class field):**

```csharp
// Arrange / Act
var result = Types.InAssembly(WebAssembly)
    .That()...
    .GetResult();

// Assert
result.IsSuccessful.Should().BeTrue();
```

See `.squad/decisions/inbox/gimli-xunit-v3-migration-pattern.md` for full details.

### Parallel execution is safe for NetArchTest

NetArchTest rules share no mutable state. Enabling `parallelizeAssembly: true`
and `parallelizeTestCollections: true` in `xunit.runner.json` is safe and
reduces execution time. This applies to any future test project using NetArchTest.

### Wave-based fan-out is effective for multi-owner migrations

The three-wave model (Infrastructure → Code → Docs) with explicit blocking
dependencies outperformed a single-owner sequential approach. Two key reasons:

1. **Parallelism within waves** — Wave 3 ran Pippin (ADR) and Aragorn (retro) concurrently
2. **Accountability per wave** — each owner had a clear, bounded scope

This pattern should be the default for any cross-cutting infrastructure change.

---

## Key Decisions Recorded

The following decisions were written to the inbox during Sprint 8 and are
pending Scribe merge into `.squad/decisions.md`:

| Decision | Author | File |
|---|---|---|
| xUnit v3 Package Strategy for Architecture.Tests | Boromir | `boromir-xunit-v3-package-strategy.md` |
| xUnit v3 Migration Pattern for NetArchTest Architecture Tests | Gimli | `gimli-xunit-v3-migration-pattern.md` |
| xUnit v3 Rollout Strategy (Architecture → Blazor) | Aragorn | `aragorn-xunit-v3-rollout-strategy.md` |

---

## Next Steps & Recommendations

### Should Blazor.Tests adopt xUnit v3 next?

**Yes — recommended for Sprint 9.** The two projects with the most complex
migration surface are `Unit.Tests` and `Blazor.Tests`. `Blazor.Tests` uses
bUnit which has explicit xUnit v3 support. Migrating it next would complete
the pilot phase and establish the bUnit + xUnit v3 pattern.

**Suggested Sprint 9 scope:**
- Wave 1 (Boromir): Update `Blazor.Tests` packages
- Wave 2 (Gimli): Migrate test code, confirm bUnit compatibility
- Wave 3 (Pippin): ADR, (Aragorn): Retrospective

### Should Unit.Tests be migrated alongside or after Blazor.Tests?

`Unit.Tests` and `Blazor.Tests` can potentially be done in the same sprint
in parallel waves if CI capacity allows. If sequenced, do `Blazor.Tests` first
(higher value, more interesting migration surface) and `Unit.Tests` second.

### Standardize on xUnit v3 for all new test projects

Any new test project created after Sprint 8 should start with xUnit v3 from
the beginning. The migration playbook is proven. There is no reason to create
new xUnit v2 projects.

### Tech debt: populate Sprint 7 retrospective with actual data

The `docs/sprint-7-xunit-v3-pilot-retro.md` file still contains `_TBD_`
placeholders. A future low-priority task should back-fill it with actual
Sprint 7 metrics (42 domain tests, CI outcomes, lessons from first migration).

### Address the planned-vs-actual scope drift in decisions.md

Update `.squad/decisions.md` to note that `Architecture.Tests` was migrated
in Sprint 8 (not `Unit.Tests` as the Sprint 7 retro template suggested). Keep
the decisions log authoritative.

---

## Timeline & Metrics

| Metric | Value |
|---|---|
| Total sprint issues | 6 (#176–#181) |
| Total PRs merged (Waves 1–2) | 4 (PRs #182, #183, and Wave 2 PRs) |
| Architecture.Tests — test count | 11 |
| Architecture.Tests — run time | 72ms (parallel) |
| Domain.Tests — test count | 42 |
| Combined passing tests | 53+ |
| xUnit v3 version | 3.2.2 |
| xunit.analyzers version | 1.27.0 |
| CI status | ✅ All green |
| Blocking issues between waves | 0 |
| Tests requiring attribute changes | 0 (NetArchTest uses only `[Fact]`) |
| Rework PRs | 0 |

---

## Team Acknowledgements

- **Boromir** — delivered a clean package foundation that made the rest of
  the sprint possible. The proactive fix to include `Domain.Tests` in CI was
  exactly the right call.
- **Gimli** — the zero-rework migration confirms the Sprint 7 playbook was
  well-designed. The AAA comment pattern will save future maintainers time.
- **Pippin** — ADR documentation in progress; the decision record will make
  the rollout pattern discoverable for future squad members.

---

## Appendix: xUnit v3 Rollout Status Across MyBlog

| Test Project | Status | Sprint |
|---|---|---|
| `tests/Domain.Tests` | ✅ Migrated | Sprint 7 |
| `tests/Architecture.Tests` | ✅ Migrated | Sprint 8 |
| `tests/Blazor.Tests` | 🔜 Recommended next | Sprint 9 |
| `tests/Unit.Tests` | 🔜 Planned | Sprint 9–10 |
| `tests/Integration.Tests` | ⚠️ Docker-dependent — assess separately | TBD |
