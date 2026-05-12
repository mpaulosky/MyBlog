# 02.01-retarget-projects: Retarget all 10 projects from net10.0 to net11.0

# 02.01-retarget-projects: Retarget all projects to net11.0

## Objective
Update the TargetFramework property in all 10 project files from net10.0 to net11.0 and build the solution to validate the retargeting.

## Scope
Affects all 10 projects:
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

## Done when
All projects have `<TargetFramework>net11.0</TargetFramework>`, solution builds without compilation errors, restore succeeds.

## Research Notes
- All 10 projects are SDK-style and currently target `net10.0` with singular `TargetFramework` entries.
- No `Directory.Build.props` override for TargetFramework was detected; each project file can be updated directly.
- This subtask scope is only TFM retargeting; package deprecation and warning cleanup are handled in later subtasks.
