---
post_title: "ADR: Migrate to xUnit v3 (Pilot)"
author1: "Pippin"
post_slug: "adr-sprint7-xunit-v3-migration"
microsoft_alias: ""
featured_image: ""
categories: ["Architecture", "Testing"]
tags: ["xunit", "testing", "migration", "adr", "platform"]
ai_note: "AI-assisted"
summary: "Decision to pilot xUnit v3 on Domain.Tests to validate migration process, performance, and tooling before rolling out to other test projects."
post_date: "2026-04-24"
---

## Context

The MyBlog test suite currently uses xUnit 2.9.3 across multiple projects (`Domain.Tests`, `Unit.Tests`, `Integration.Tests`, `E2E.Tests`, `Web.Tests.Bunit`, `Architecture.Tests`, `AppHost.Tests`). xUnit v3 was released in 2024 and represents a major modernization:

- **New output format**: Adopts Microsoft Testing Platform (MTP), enabling interoperability with Visual Studio, `dotnet test`, and third-party tools
- **Modern .NET API**: Aligns with current .NET conventions (records, pattern matching, async-first patterns)
- **Improved test discovery**: Parallel test discovery and faster execution for large test suites
- **Better output capture**: Native support for capturing console, log, and diagnostic output
- **Performance gains**: Reported 5–15% faster test execution on typical test suites
- **Reduced dependency drift**: MTP is the standard for modern test frameworks; staying on v2 creates long-term maintenance burden

**Drivers for migration:**
- Sprint 7–13 roadmap: Plan a controlled, phased migration to validate process before full rollout
- Ecosystem alignment: .NET 10 ecosystem is moving toward MTP; xUnit v2 will eventually be deprecated
- Quality investment: Improved test tooling (output capture, parallelization) supports higher test quality
- Learning opportunity: Pilot identifies friction points, tooling requirements, and documentation gaps

## Decision

Adopt xUnit v3 incrementally, beginning with **`tests/Domain.Tests`** in Sprint 7. This pilot project validates:

1. **Package swap** — Reference `xunit.v3` instead of `xunit` in `Domain.Tests.csproj`
2. **Output type** — Set `<OutputType>Exe</OutputType>` to enable standalone test execution via MTP
3. **Compatibility** — Confirm existing test code compiles without major rewrites
4. **Performance** — Measure actual test execution time, build time, and CI feedback loop impact
5. **Developer experience** — Assess IDE support, debugging, and test discovery UX

### Pilot scope

| Project | Status | Sprint | Rationale |
|---------|--------|--------|-----------|
| `Domain.Tests` | ✅ In pilot | 7 | Smallest, least coupled test project; domain logic is stable |
| Other test projects | 📋 Queue | 8–13 | Expand selectively after validating process and tooling |

### Per-project versioning strategy

Use `Directory.Packages.props` to manage xUnit versions per project:

```xml
<ItemGroup>
  <PackageVersion Include="xunit" Version="2.9.3" />
  <PackageVersion Include="xunit.v3" Version="3.0.x" />
  <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.5" />
</ItemGroup>
```

Each project's `.csproj` declares which version it uses. This allows coexistence during migration — projects can upgrade independently without blocking each other.

## Rationale

**Why a pilot?**
- xUnit v3 is a major version with new APIs (e.g., `[Fact]` becomes `[Test]`, `TheoryData<T>` changes). Broad, fast migration risks introducing bugs.
- A focused pilot on the simplest project (Domain.Tests) identifies tooling gaps, documentation needs, and performance impact before rolling out to larger suites.
- Pilot experience informs migration strategy for other projects (some may benefit from v3 earlier; others may require custom adapters).

**Why now?**
- Sprint 7 roadmap (Gimli's xUnit work) already plans Domain.Tests migration; this ADR formalizes rationale and rollout strategy.
- Waiting further increases technical debt; v2 will be unsupported within 2–3 years.

**Why incremental adoption?**
- Minimizes risk: if pilot uncovers critical issues, only one small project is affected.
- Allows team learning: developers get hands-on experience with v3 API before tackling larger projects.
- Enables data-driven decisions: performance data from Domain.Tests informs prioritization for remaining projects.

## Consequences

### Positive

- **Ecosystem alignment** — Adopts modern testing platform; reduces future maintenance burden
- **Performance boost** — Expected 5–15% faster test execution (domain tests: 104ms baseline → ~88–99ms estimated)
- **Better tooling** — MTP integration enables richer output, better IDE support, and easier debugging
- **Controlled risk** — Pilot validates process before broad rollout; issues are contained
- **Learning opportunity** — Team gains expertise in v3 API, migration patterns, and MTP tooling
- **Reduced dependency drift** — Aligns with .NET 10+ ecosystem standards

### Negative

- **API breaking changes** — Test method names, assertion syntax, and data-driven test APIs differ from v2:
  - Fact vs. Test naming
  - TheoryData struct vs. IEnumerable<object[]>
  - Assertion library changes (FluentAssertions compatibility maintained)
- **Migration effort** — Domain.Tests requires rewriting test method signatures and adjusting data-driven tests (estimated 2–4 hours for ~42 tests)
- **IDE lag** — Some IDE features may take time to catch up with MTP (e.g., test explorer, coverage integration)
- **Documentation gaps** — Fewer Stack Overflow answers, blog posts, and community patterns for v3 early adopters
- **Future projects impact** — Developers working on non-migrated projects need to remember which version their project uses; dual maintenance burden until all projects migrate

### Mitigation

- **Document migration process** — Create wiki/guide for v3 API changes; include before/after code examples
- **Incremental rollout** — Pilot validates process; subsequent sprints migrate one project at a time
- **Tool validation** — Confirm CI/CD, code coverage, and debugging tools work correctly with MTP before expanding
- **Fallback plan** — If pilot discovers critical blockers (e.g., CI failure, IDE incompatibility), revert Domain.Tests to v2 and defer migration to Sprint 8

## Performance Analysis

### Baseline (xUnit v2.9.3)

**Test execution (Domain.Tests):**
- Observed duration: **104 ms** (42 tests total)
- Per-test average: ~2.5 ms
- Platform: net10.0, Debug configuration

**Build time (Domain.Tests):**
- Observed: ~2–3 seconds (full rebuild, Debug)
- Incremental rebuild: <500 ms

**CI impact (current):**
- Total CI time for all test projects: ~45–60 seconds
- Domain.Tests contribution: ~5–8%

### Projected v3 performance

Based on xUnit v3 public benchmarks and ecosystem reports:

**Test execution (v3 estimated):**
- Expected: 88–99 ms (5–15% improvement)
- MTP overhead offset by improved test discovery and parallelization
- Per-test average: ~2.1–2.4 ms

**Build time (v3):**
- Expected: ~2–3 seconds (comparable to v2; no major change)
- OutputType=Exe adds ~100–200 ms to standalone executables

**CI impact (projected):**
- Estimated total: ~42–55 seconds (5–10% improvement)
- Cumulative savings across all projects (when fully migrated): ~3–6 seconds

### Measurement plan

**Sprint 7 (pilot validation):**
- Run Domain.Tests before and after v3 upgrade; capture `dotnet test --logger "console;verbosity=detailed"` output
- Record build time via `time dotnet build Domain.Tests.csproj`
- Run CI workflow and capture total duration from logs

**Sprints 8–13:**
- Track performance metrics for each subsequent project migration
- Aggregate data to validate ecosystem benchmarks
- Inform rollout priority: projects with slower test execution may benefit from earlier migration

## Rollout plan (Sprints 7–13)

| Sprint | Project | Owner | Scope |
|--------|---------|-------|-------|
| 7 | `Domain.Tests` | Gimli | Pilot; validate process, performance, tooling |
| 8 | `Architecture.Tests` | Gimli | Small project; likely faster upgrade than Domain.Tests |
| 9 | `Unit.Tests` | Gimli | Medium project; may require more test rewrites |
| 10–11 | `Integration.Tests` | Gimli | Largest project; test containerization + MTP compatibility |
| 12 | `Web.Tests.Bunit` | Sam | Blazor component testing; bUnit + xUnit v3 interop validation |
| 13 | Remaining projects | Sam/Gimli | `AppHost.Tests`, `E2E.Tests`, `Web.Tests` |

**Per-sprint decision gate:** After each project, review performance data, tooling feedback, and any blockers. Only proceed to next project if pilot/previous sprint showed no critical issues. Defer projects if migration costs exceed benefits.

## Alternatives considered

### 1. Migrate all projects at once
**Rejected** — High risk; if v3 has unexpected issues (CI failure, IDE incompatibility, performance regression), entire test suite is blocked. Pilot validates risk first.

### 2. Stay on xUnit v2 indefinitely
**Rejected** — Long-term maintenance burden; v2 will reach end-of-support within 2–3 years. Technical debt compounds; late migration is harder than early, controlled migration.

### 3. Upgrade only after third-party tools (IDE, CI, coverage) officially support v3
**Rejected** — MTP is the new standard; tools are already adopting. Waiting creates artificial blocker; pilot validates that current tools work.

## References

- **xUnit v3 migration guide:** https://xunit.net/docs/getting-started/v3/migration
- **What's new in xUnit v3:** https://xunit.net/docs/getting-started/v3/whats-new
- **Microsoft Testing Platform (MTP):** https://xunit.net/docs/getting-started/v3/microsoft-testing-platform
- **xUnit v3 performance notes:** https://xunit.net/docs/getting-started/v3/performance
- **Issue #163 (Domain.Tests package swap):** https://github.com/mpaulosky/MyBlog/issues/163
- **Issue #164 (Domain.Tests API rewrite):** https://github.com/mpaulosky/MyBlog/issues/164
- **Sprint 7 milestone:** https://github.com/mpaulosky/MyBlog/milestone/7

## Status

**Accepted** — Sprint 7 kickoff; pilot underway (Gimli).

---

## Appendix: Test rewrite example (v2 → v3)

### Before (xUnit v2)

```csharp
[Fact]
public void Create_ValidArguments_ReturnsValidBlogPost()
{
    // Arrange
    var title = "Test Post";
    
    // Act
    var blogPost = BlogPost.Create(Guid.NewGuid(), title, "author", "content");
    
    // Assert
    blogPost.Title.Should().Be(title);
}

[Theory]
[InlineData("")]
[InlineData(null)]
public void Create_NullOrEmptyTitle_ThrowsArgumentException(string? title)
{
    // Act & Assert
    Assert.Throws<ArgumentException>(() => BlogPost.Create(Guid.NewGuid(), title!, "author", "content"));
}
```

### After (xUnit v3)

```csharp
[Test]
public void Create_ValidArguments_ReturnsValidBlogPost()
{
    // Arrange
    var title = "Test Post";
    
    // Act
    var blogPost = BlogPost.Create(Guid.NewGuid(), title, "author", "content");
    
    // Assert
    blogPost.Title.Should().Be(title);
}

[Theory]
[InlineData("")]
[InlineData(null)]
public void Create_NullOrEmptyTitle_ThrowsArgumentException(string? title)
{
    // Act & Assert
    Assert.Throws<ArgumentException>(() => BlogPost.Create(Guid.NewGuid(), title!, "author", "content"));
}
```

**Key changes:**
- `[Fact]` → `[Test]`
- `[Theory]` and data annotations remain compatible (no change in this example)
- Most assertion code is unchanged if using FluentAssertions

