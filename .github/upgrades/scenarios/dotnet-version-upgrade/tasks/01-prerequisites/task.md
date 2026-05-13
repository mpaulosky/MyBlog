# 01-prerequisites: Prepare toolchain and environment

Verify .NET SDK compatibility with the target framework. Check that the development environment has or can obtain .NET 11 SDK support. Update `global.json` to align with the target framework requirements. Confirm all projects reference compatible tool versions. Create a baseline test pass on the current framework to establish a known-good state before any framework changes.

**Done when**: `global.json` updated, .NET 11 SDK validated as available, baseline tests pass on current framework

## Scope Inventory

**Projects affected**: All 10 projects depend on the toolchain configuration

**Distinct concerns**:

1. .NET SDK verification (.NET 11 availability)
2. `global.json` update (SDK version pinning)
3. Baseline validation (tests pass on current framework before any upgrade)

**Change signals**:

- Current SDK: 10.0.200 (latest for .NET 10)
- Available SDKs: 8.0, 9.0 (x3), 10.0 (x3), 10.0.300-preview
- Target: .NET 11.0 (Preview) — no preview SDK found yet
- Current `global.json`: pinned to 10.0.200 with rollForward: latestMinor, allowPrerelease: false

**Research findings**:

- .NET 11 SDK is not currently installed. Check `dotnet --list-sdks` — only 10.0 (and preview) available
- `global.json` prevents prerelease SDKs with `allowPrerelease: false`
- Need to update `global.json` to allow prerelease and possibly adjust rollForward
- Baseline build/test validation needed on current state before making any changes
