# E2E Test Infrastructure Fix - AppHost.Tests Aspire Timeout Resolution

**Date:** 2026-04-23
**Status:** ✅ RESOLVED
**Branch:** squad/80-reorganize-test-projects
**Commit:** 89fd3dc

## Problem

AppHost.Tests E2E tests were **timing out during Aspire fixture initialization** with the error:

```
System.TimeoutException: Web app at https://localhost:7043/ was not ready after 180s
```

AspireManager.StartAppAsync() was unable to verify the web app's health because the `/alive` endpoint was not responding.

### Root Causes

After detailed investigation with added logging, two critical issues were identified:

#### 1. **Missing /alive Endpoint in Testing Environment**

The health check endpoints (`/alive` and `/health`) were **only mapped in Development** mode:

```csharp
// src/ServiceDefaults/Extensions.cs - OLD
if (app.Environment.IsDevelopment())
{
    app.MapHealthChecks(HealthEndpointPath);
    app.MapHealthChecks(AlivenessEndpointPath, ...);
}
```

When the web app ran in **Testing environment** (as configured by AspireManager), the endpoints didn't exist, causing connection refusal errors.

#### 2. **Auth0 Configuration Throwing Exception**

The web app crashed on startup with:

```
System.ArgumentException: The value cannot be an empty string. (Parameter 'ClientId')
```

The issue was in Web/Program.cs Auth0 configuration:

```csharp
// OLD - fails when Configuration returns empty string (not null)
else if (string.IsNullOrEmpty(auth0Domain) || string.IsNullOrEmpty(auth0ClientId))
{
    auth0Domain ??= "test.auth0.com";      // Doesn't help if auth0Domain is ""
    auth0ClientId ??= "test-client-id";    // Doesn't help if auth0ClientId is ""
}
```

Configuration["Auth0:ClientId"] returns an **empty string** `""` when the setting isn't configured, not `null`. The null coalescing operator `??=` only replaces `null`, so empty strings passed through to Auth0 library → ArgumentException.

## Solution

### 1. Enable /alive Endpoint for Testing Environment

**File:** `src/ServiceDefaults/Extensions.cs`

```csharp
// NEW - Map health endpoints for both Development and Testing
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.MapHealthChecks(HealthEndpointPath);
    app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("live")
    });
}
```

**Why this works:**
- The Testing environment now has the same health endpoint infrastructure as Development
- AspireManager can successfully poll `/alive` to verify app readiness
- The "live" health check (just "self" liveness) completes quickly without waiting for external services

### 2. Fix Auth0 Configuration for Empty String Values

**File:** `src/Web/Program.cs`

```csharp
// NEW - Handle both null AND empty string
else if (string.IsNullOrWhiteSpace(auth0Domain) || string.IsNullOrWhiteSpace(auth0ClientId))
{
    // Development/Testing: Use test/mock values if not configured
    auth0Domain = "test.auth0.com";
    auth0ClientId = "test-client-id";
}
```

**Changes:**
- `string.IsNullOrEmpty()` → `string.IsNullOrWhiteSpace()` (also catches empty strings)
- `??=` (null coalescing) → Direct assignment (ensures override of empty strings)

### 3. Add Comprehensive Logging to AspireManager

**File:** `tests/AppHost.Tests/Infrastructure/AspireManager.cs`

Added detailed logging at key stages:
- Aspire app creation and configuration
- Web endpoint discovery
- Polling attempts with elapsed time and attempt count
- Response status codes
- Specific exception details for troubleshooting

Example output from logs:

```
Starting AppHost Aspire application...
Setting ASPNETCORE_ENVIRONMENT=Testing
Creating DistributedApplicationTestingBuilder...
Builder created successfully. Configuring...
Injecting ASPNETCORE_ENVIRONMENT=Testing into web resource...
Fixing web endpoint port to 7043...
Building Aspire application...
Aspire application built successfully
Starting Aspire application services...
Aspire application started successfully
Waiting for web app to become healthy...
Web endpoint discovered: https://localhost:7043/
Attempt 1 (elapsed 0.0s): Polling https://localhost:7043//alive
Attempt 2 (elapsed 1.0s): Polling https://localhost:7043//alive
Attempt 3 (elapsed 2.0s): Polling https://localhost:7043//alive
Response status: OK
Web app is healthy! Status: OK
Web app is healthy and ready for tests
```

## Testing & Verification

After applying the fixes:

✅ **Infrastructure tests now execute successfully:**
- Aspire app starts without timeout
- Web app becomes healthy and accessible
- Playwright browser initializes correctly
- Several E2E tests now pass:
  - WebPlaywrightTests.WebHomePageLoads
  - NotFoundPageTests.NotFoundPage_ShowsHelpfulMessage
  - NotFoundPageTests.NotFoundPage_ShowsNotFoundHeading
  - IssueIndexPageTests.IssueIndexPage_LoadsWithoutRedirect
  - DashboardPageTests.DashboardPage_LoadsWithoutRedirect

✅ **Build succeeds with no errors**

Remaining test failures are application-specific (missing UI elements, page content issues) - not infrastructure problems.

## Security Considerations

**Testing Environment Health Endpoints:**
- Only enabled in Development and Testing environments
- Not exposed in Production
- `/health` and `/alive` provide minimal information (no sensitive data)
- Follows ASP.NET Core best practices

## Files Changed

1. `src/ServiceDefaults/Extensions.cs` - MapDefaultEndpoints method
2. `src/Web/Program.cs` - Auth0 configuration fallback logic
3. `tests/AppHost.Tests/Infrastructure/AspireManager.cs` - Added comprehensive logging

## Future Improvements

1. Consider extracting logging setup to avoid duplicating ILogger creation
2. Document the Testing environment's special configuration requirements
3. Monitor logs for any recurring startup issues
4. Consider health check timeout optimization for CI/CD cold starts

## References

- [ASP.NET Core Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [Aspire Testing Best Practices](https://aka.ms/dotnet/aspire/testing)
- [String.IsNullOrWhiteSpace vs IsNullOrEmpty](https://learn.microsoft.com/en-us/dotnet/api/system.string.isnullorwhitespace)
