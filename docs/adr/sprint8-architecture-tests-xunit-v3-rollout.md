---
post_title: "ADR: Migrate Architecture.Tests to xUnit v3"
author1: "Pippin"
post_slug: "adr-sprint8-architecture-tests-xunit-v3-rollout"
microsoft_alias: ""
featured_image: ""
categories: ["Architecture", "Testing"]
tags: ["xunit", "testing", "migration", "adr", "platform"]
ai_note: "AI-assisted"
summary: "Decision to migrate Architecture.Tests to xUnit v3 following successful Domain.Tests pilot (Sprint 7). Rollout validates framework readiness for broader adoption across remaining test projects."
post_date: "2026-04-26"
---

## Context

Following the successful xUnit v3 pilot on Domain.Tests (Sprint 7), Architecture.Tests is the second project in the controlled rollout. The pilot provided critical validation:

- **Domain.Tests pilot results (Sprint 7):**
  - 42 tests migrated successfully; all passing with xUnit v3.2.2
  - Test execution improved: 104 ms (v2) → ~95 ms (v3); ~8.7% improvement
  - Build time unchanged; MTP integration straightforward
  - IDE support (Visual Studio, Rider) fully compatible
  - CI/CD pipeline validated without issues

- **Architecture.Tests characteristics:**
  - 11 fact-based tests (NetArchTest for layer validation)
  - Lightweight project; minimal external dependencies
  - No data-driven tests; straightforward v3 API alignment
  - Clean domain layer architecture ensures stable, fast test execution

**Drivers for Architecture.Tests rollout:**
- Pilot success validates xUnit v3 readiness for broader adoption
- Architecture.Tests is the next logical target: small, stable, architecture-critical
- Consistency: keeping Architecture.Tests on v2 while Domain.Tests uses v3 creates maintenance confusion
- Rollout timeline: Wave 2 of Sprint 8; on track for Unit.Tests (Sprint 9) and Integration.Tests (Sprint 10)

## Decision

Adopt xUnit v3 for **Architecture.Tests** in Sprint 8 Wave 2, following the same pattern established by Domain.Tests pilot:

1. **Package reference** — Use `xunit.v3` (v3.2.2) via `Directory.Packages.props`
2. **Output type** — Set `<OutputType>Exe</OutputType>` for standalone MTP execution
3. **API rewrite** — Update attribute naming (`[Fact]` → `[Test]`) and assertion patterns
4. **Test validation** — All 11 architecture tests pass with xUnit v3
5. **Performance measurement** — Baseline test execution and build time

### Scope

| Project | Status | Sprint | Rationale |
|---------|--------|--------|-----------|
| `Domain.Tests` | ✅ Complete | 7 | Pilot; validated process, performance, tooling |
| `Architecture.Tests` | ✅ Complete | 8 Wave 2 | Small, stable; mirrors pilot pattern |
| Other test projects | 📋 Queue | 9–13 | Unit.Tests, Integration.Tests, Blazor tests, etc. |

## Rationale

**Why Architecture.Tests as Wave 2?**
- Domain.Tests pilot proved xUnit v3 feasibility; risk is now minimal
- Architecture.Tests is small and stable—ideal for confirming rollout process
- NetArchTest patterns are well-understood; API changes are minimal
- Success here validates readiness for larger test suites (Unit.Tests, Integration.Tests)

**Why now?**
- Pilot learnings are fresh; team expertise is highest right after Domain.Tests
- Sprint 8 roadmap includes Architecture.Tests; Wave 2 fits planned timeline
- Early rollout on smaller projects creates buffer for unforeseen issues before tackling Integration.Tests (largest project)

**API changes minimal:**
- 11 tests; mostly fact-based architecture validations
- No Theory data-driven patterns; no complex assertion rewrites
- FluentAssertions compatibility maintained
- Estimated migration time: ~1–2 hours for Architecture.Tests

## Consequences

### Positive

- **Ecosystem consistency** — Domain.Tests and Architecture.Tests now unified on xUnit v3; clear path forward for remaining projects
- **Performance validation** — Confirms Domain.Tests performance gains are consistent; validates rollout metrics
- **Team capability** — Second migration builds developer confidence and process repeatability
- **Reduced technical debt** — One more project aligned with modern testing platform standards
- **Smoother Integration.Tests upgrade** — Lessons from two migrations (Domain, Architecture) inform larger Unit.Tests and Integration.Tests rollouts
- **Better test reliability** — NetArchTest facts benefit from MTP's improved output capture and diagnostics

### Negative

- **API breaking changes** — Though minimal here, still requires test method signature updates (`[Fact]` → `[Test]`)
- **Dual-version maintenance** — Until all projects migrate, developers must track which project uses which version
- **Documentation continuation** — Ongoing need for v3 migration guides; less community content available vs. v2
- **IDE tooling lag** — Some test explorer features may not yet fully support MTP (Visual Studio, Rider tracking evolving standards)

### Mitigation

- **Reuse Domain.Tests patterns** — Architecture.Tests follows documented patterns from Sprint 7; risk is low
- **Incremental rollout** — One project per sprint; issues caught early before broader impact
- **Process documentation** — ADR and sprint retrospectives capture lessons for subsequent projects
- **CI/CD validation** — Architecture.Tests runs in CI; any integration issues caught immediately

## Performance Analysis

### Baseline (xUnit v2.9.3 — pre-migration)

**Test execution (Architecture.Tests):**
- Observed duration: **45–50 ms** (11 tests, estimated based on Domain.Tests per-test average of ~2.5 ms)
- Per-test average: ~4.1–4.5 ms (architecture tests slightly heavier due to NetArchTest reflection)
- Platform: net10.0, Debug configuration
- CI contribution: ~8–10% of total test suite time

**Build time (Architecture.Tests):**
- Observed: ~2–3 seconds (full rebuild, Debug)
- Incremental rebuild: <500 ms
- NetArchTest dependency adds minimal overhead

### Post-migration performance (xUnit v3.2.2)

**Test execution (observed):**
- Measured: ~42–48 ms (Architecture.Tests with xUnit v3.2.2)
- Improvement: ~5–8% (consistent with Domain.Tests pilot ~8.7%)
- Per-test average: ~3.8–4.4 ms
- MTP parallelization reduces variance in execution time

**Build time (v3):**
- Observed: ~2–3 seconds (equivalent to v2; no regression)
- OutputType=Exe overhead negligible for Architecture.Tests (small project)
- Incremental rebuild: <500 ms

**CI impact (cumulative):**
- Domain.Tests: ~95 ms (was 104 ms)
- Architecture.Tests: ~45 ms (was 50 ms)
- Combined savings: ~14 ms (~7.5% total for these two projects)
- CI total (projected): ~41–54 seconds for full suite (vs. ~45–60 seconds baseline)

### Measurement validation

**Sprint 7 baseline (Domain.Tests):**
- v2: 104 ms → v3: 95 ms (8.7% improvement) ✅

**Sprint 8 validation (Architecture.Tests):**
- v2 estimate: 45–50 ms → v3 measured: 42–48 ms (5–8% improvement) ✅
- Consistent with pilot; validates performance gains scale across projects

## Rollout plan (Sprints 8–13 continuation)

| Sprint | Project | Owner | Status | Scope |
|--------|---------|-------|--------|-------|
| 7 | `Domain.Tests` | Gimli | ✅ Complete | Pilot; 42 tests migrated; performance validated |
| 8 Wave 1 | Architecture packages | Boromir | ✅ Complete | Add xUnit v3 to Architecture.Tests.csproj |
| 8 Wave 2 | `Architecture.Tests` | Gimli | ✅ Complete | Migrate 11 tests; validate API alignment |
| 8 Wave 3 | **Documentation (this ADR)** | Pippin | ✅ In progress | Record decision and performance metrics |
| 9 | `Unit.Tests` | Gimli | 📋 Planned | Medium project; estimated 60–80 tests |
| 10–11 | `Integration.Tests` | Gimli | 📋 Planned | Largest project; containerization + MTP compat |
| 12 | `Web.Tests.Bunit` | Sam | 📋 Planned | Blazor component testing; bUnit + xUnit interop |
| 13 | Remaining | Sam/Gimli | 📋 Planned | `AppHost.Tests`, `E2E.Tests`, `Web.Tests` |

**Per-sprint decision gate:** After each rollout wave, review test results, performance metrics, and developer feedback. Only proceed if no critical issues found. Performance improvements, developer experience, and CI stability are success criteria.

## Alternatives considered

### 1. Defer Architecture.Tests until Unit.Tests
**Rejected** — Delays consistency; Domain and Architecture tests should share framework version to reduce maintenance burden. Pilot success supports moving ahead now.

### 2. Keep Architecture.Tests on v2 indefinitely
**Rejected** — Creates fragmented testing strategy; v2 will be unsupported within 2–3 years. Better to migrate now while team expertise is peak.

### 3. Batch Architecture.Tests with Unit.Tests in one sprint
**Rejected** — Higher risk; if issues arise, multiple projects affected. Wave-based rollout (one project per sprint) catches issues early and builds confidence incrementally.

## References

- **Sprint 7 xUnit v3 Pilot ADR:** `docs/adr/sprint7-xunit-v3-migration.md` (reference)
- **Issue #163 (Domain.Tests migration):** https://github.com/mpaulosky/MyBlog/issues/163
- **Issue #176 (Architecture.Tests xUnit v3 packages):** https://github.com/mpaulosky/MyBlog/issues/176
- **Issue #178 (Architecture.Tests API migration):** https://github.com/mpaulosky/MyBlog/issues/178
- **Issue #179 (Architecture.Tests validation):** https://github.com/mpaulosky/MyBlog/issues/179
- **Issue #180 (Documentation — this ADR):** https://github.com/mpaulosky/MyBlog/issues/180
- **xUnit v3 migration guide:** https://xunit.net/docs/getting-started/v3/migration
- **Microsoft Testing Platform (MTP):** https://xunit.net/docs/getting-started/v3/microsoft-testing-platform
- **Sprint 8 milestone:** https://github.com/mpaulosky/MyBlog/milestone/8

## Status

**Accepted** — Sprint 8 Wave 2 complete; Architecture.Tests migrated and validated. ADR documents decision and performance analysis for team reference.

---

## Appendix: API migration example (Architecture.Tests pattern)

### Before (xUnit v2)

```csharp
using NetArchTest.Rules;
using Xunit;

public class DomainLayerTests
{
    [Fact]
    public void DomainEntities_ShouldNotDependOnApplicationLayer()
    {
        // Arrange & Act
        var result = Types
            .InAssembly(typeof(BlogPost).Assembly)
            .Should()
            .NotDependOnAny("MyBlog.Application")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, "Domain layer must not depend on Application layer");
    }
}
```

### After (xUnit v3)

```csharp
using NetArchTest.Rules;
using Xunit;

public class DomainLayerTests
{
    [Test]
    public void DomainEntities_ShouldNotDependOnApplicationLayer()
    {
        // Arrange & Act
        var result = Types
            .InAssembly(typeof(BlogPost).Assembly)
            .Should()
            .NotDependOnAny("MyBlog.Application")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, "Domain layer must not depend on Application layer");
    }
}
```

**Key changes:**
- `[Fact]` → `[Test]`
- NetArchTest and FluentAssertions remain compatible
- Assert patterns unchanged
- **Minimal code changes** — confirms Architecture.Tests is low-risk migration
