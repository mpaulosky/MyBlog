# .NET Version Upgrade Plan

## Overview

**Target**: Upgrade 10 projects from .NET 10.0 to .NET 11.0 (Preview)

**Scope**: Small solution, all SDK-style, all modern .NET. Straightforward version bump with one deprecated package to address. ~12k LOC across foundation libraries, business logic, web app, and comprehensive test suite. All projects are directly or indirectly related through the Aspire AppHost orchestrator.

## Tasks

### 01-prerequisites: Prepare toolchain and environment

Verify .NET SDK compatibility with the target framework. Check that the development environment has or can obtain .NET 11 SDK support. Update `global.json` to align with the target framework requirements. Confirm all projects reference compatible tool versions. Create a baseline test pass on the current framework to establish a known-good state before any framework changes.

**Done when**: `global.json` updated, .NET 11 SDK validated as available, baseline tests pass on current framework

---

### 02-upgrade-all-projects: Update all projects to net11.0

Update all 10 project files to target `net11.0`. This includes: AppHost (orchestrator), Domain (business logic library), ServiceDefaults (shared configuration), Web (Blazor frontend), and their corresponding test projects (AppHost.Tests, Architecture.Tests, Domain.Tests, Web.Tests, Web.Tests.Bunit, Web.Tests.Integration).

Update all NuGet package references across the solution. Address the one deprecated package (FluentValidation.AspNetCore). Fix any API compatibility issues flagged in the assessment (3 potential behavioral/source incompatibilities in Web and test projects). Restore dependencies and validate solution builds without errors or warnings. Run full test suite to verify functionality across all test projects.

**Done when**: All projects target `net11.0`, solution builds cleanly with zero warnings, all tests pass, no breaking API changes remain unaddressed

---

### 03-final-validation: Comprehensive solution verification

Run full build on release configuration. Execute complete test suite (unit, bUnit, integration, and architecture tests). Verify Aspire app orchestration still functions correctly. Confirm no regressions in feature functionality. Document any known limitations or deferred recommendations.

**Done when**: Release build succeeds with zero errors/warnings, all tests pass, AppHost orchestration works, application is ready for deployment
