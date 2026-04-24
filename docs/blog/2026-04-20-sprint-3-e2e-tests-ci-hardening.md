---
title: "Sprint 3: E2E Testing and CI Hardening"
date: 2026-04-20
author: Bilbo
tags: [e2e, aspire, ci, testing, sprint-3]
summary: "Sprint 3 adds end-to-end testing with .NET Aspire xUnit and hardens the CI pipeline for squad branch workflows."
---

## Summary

Sprint 3 strengthens quality and automation. We added the E2E.Tests project using .NET Aspire's xUnit integration, introduced visual distinction via Admin badge styling and RoleClaimsHelper, and hardened CI to enforce squad branch naming on sprint/* branches. Test artifacts now upload to CI runs, GitHub project board automation moves issues to Released, and the pre-push Gate 0 validates branch names. Release v1.0.0-sprint3 marks our first production-ready milestone.

## Context

As the project grows, we need multiple testing layers:
- **Unit tests** verify business logic in isolation
- **Integration tests** check persistence and service wiring
- **E2E tests** simulate real user workflows in a live environment

.NET Aspire provides the orchestration context for E2E tests, spinning up the full application and letting xUnit run scenarios end-to-end. CI automation ensures consistency—artifact uploads let us inspect test results post-run, and branch name validation prevents accidental merges to main.

## Key Details

### E2E Testing with .NET Aspire
The E2E.Tests project uses AppHost's resource definitions to spin up the application:
```csharp
[TestClass]
public class BlogPostE2ETests
{
    private readonly AspireTestExtension _aspire = new();

    [TestMethod]
    public async Task GetBlogPosts_ReturnsAllPosts()
    {
        var httpClient = _aspire.CreateHttpClient("webfrontend");
        var response = await httpClient.GetAsync("/api/blog-posts");
        Assert.IsTrue(response.IsSuccessStatusCode);
    }
}
```

### UI Enhancements
- **Admin Badge**: Visual indicator for admin users in profile
- **Role-Colored Badges**: Different colors for different roles (admin, editor, reader)
- **RoleClaimsHelper**: Utility to safely parse and validate claims

**Related PR:** #79

### CI/CD Hardening

#### Squad CI on Sprint Branches
- Enabled squad-test CI workflow on sprint/* branches (PR #70)
- Test results artifact uploads (PR #75)

#### Pre-Push Gate Validation
- Branch naming enforcement via Gate 0 (PRs #74, #78)
- Prevents commits to main; validates sprint/* naming

#### GitHub Project Automation
- Issues automatically move to "Released" column on GitHub Release publish (PR #76)

**Related PRs:** #70, #74, #75, #76, #77, #78, #79

## What's Next

Sprint 4 introduces Blazor theme system with TailwindCSS v4 and reorganizes test infrastructure into focused, maintainable layers.

---

*Posted by Bilbo • Sprint 3 | Release: v1.0.0-sprint3*
