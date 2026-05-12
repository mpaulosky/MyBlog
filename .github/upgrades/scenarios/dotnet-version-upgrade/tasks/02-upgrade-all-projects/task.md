# 02-upgrade-all-projects: Update all projects to net11.0

Update all 10 project files to target `net11.0`. This includes: AppHost (orchestrator), Domain (business logic library), ServiceDefaults (shared configuration), Web (Blazor frontend), and their corresponding test projects (AppHost.Tests, Architecture.Tests, Domain.Tests, Web.Tests, Web.Tests.Bunit, Web.Tests.Integration).

Update all NuGet package references across the solution. Address the one deprecated package (FluentValidation.AspNetCore). Fix any API compatibility issues flagged in the assessment (3 potential behavioral/source incompatibilities in Web and test projects). Restore dependencies and validate solution builds without errors or warnings. Run full test suite to verify functionality across all test projects.

**Done when**: All projects target `net11.0`, solution builds cleanly with zero warnings, all tests pass, no breaking API changes remain unaddressed
