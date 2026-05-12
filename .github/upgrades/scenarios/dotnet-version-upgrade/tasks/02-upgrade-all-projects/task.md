# 02-upgrade-all-projects: Update all projects to net11.0

Update all 10 project files to target `net11.0`. This includes: AppHost (orchestrator), Domain (business logic library), ServiceDefaults (shared configuration), Web (Blazor frontend), and their corresponding test projects (AppHost.Tests, Architecture.Tests, Domain.Tests, Web.Tests, Web.Tests.Bunit, Web.Tests.Integration).

Update all NuGet package references across the solution. Address the one deprecated package (FluentValidation.AspNetCore). Fix any API compatibility issues flagged in the assessment (3 potential behavioral/source incompatibilities in Web and test projects). Restore dependencies and validate solution builds without errors or warnings. Run full test suite to verify functionality across all test projects.

**Done when**: All projects target `net11.0`, solution builds cleanly with zero warnings, all tests pass, no breaking API changes remain unaddressed

## Scope Inventory

**Projects affected**:
- src/AppHost/AppHost.csproj
- src/Domain/Domain.csproj
- src/ServiceDefaults/ServiceDefaults.csproj
- src/Web/Web.csproj
- tests/AppHost.Tests/AppHost.Tests.csproj
- tests/Architecture.Tests/Architecture.Tests.csproj
- tests/Domain.Tests/Domain.Tests.csproj
- tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj
- tests/Web.Tests/Web.Tests.csproj
- tests/Web.Tests.Integration/Web.Tests.Integration.csproj

**Distinct concerns**:
1. Retarget all projects from net10.0 to net11.0
2. Resolve deprecated package usage (FluentValidation.AspNetCore)
3. Address assessment API compatibility findings in Web and test projects
4. Restore/build and fix all warnings in touched projects
5. Run full test suite on net11.0

**Dependency/context signals**:
- Project graph depth: 4 levels (Domain/ServiceDefaults foundational, Web mid-tier, AppHost upper-tier, tests at top)
- Package management: CPM enabled via Directory.Packages.props
- Known package issue: FluentValidation.AspNetCore deprecated
- Known API issues: Web, Web.Tests, AppHost.Tests flagged in assessment

**Execution approach**:
This task is decomposed into focused subtasks to reduce blast radius and enforce validation gates between framework retargeting, package migration, compatibility fixes, and full test validation.
