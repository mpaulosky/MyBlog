---
name: webapp-testing
description: >
  MyBlog guidance for running-browser verification of the Blazor UI after bUnit
  coverage exists, especially for JS interop, Auth0 redirects, and AppHost smoke
  checks.
---

## MyBlog Web Application Testing

### Current repo fit

- **Automated UI coverage** lives in `tests/Unit.Tests` with **bUnit**:
  - `tests/Unit.Tests/Components/Layout/NavMenuTests.cs` — Navigation menu auth states, theme toggle, JS interop
  - `tests/Unit.Tests/Components/RazorSmokeTests.cs` — Component rendering smoke tests
  - `tests/Unit.Tests/Features/UserManagement/ProfileTests.cs` — Profile component claim assertions
  - bUnit tests use `BunitContext` (base class from bUnit; test-specific helpers in `TestAuthorizationService.cs`)

- **No** Playwright or dedicated browser-test project exists today.

- **Browser-level checks** are useful for runtime-only behavior that bUnit cannot verify:
  - Auth0 redirect wiring in `/Account/Login` and `/Account/Logout`
  - `src/Web/wwwroot/js/theme.js` persistence (local storage, CSS class toggling across page reloads)
  - AppHost smoke flows where MongoDB and Redis wiring matter for end-to-end scenarios

### MyBlog-Tested Patterns (Established)

1. **bUnit first, browser second**
   - All new UI features must have bUnit test coverage in `tests/Unit.Tests` FIRST.
   - Use browser automation ONLY when:
     - JS interop behavior cannot be fully verified in bUnit (e.g., localStorage effects)
     - Navigation or redirect chains span multiple endpoints
     - Full runtime behavior with infrastructure (AppHost) is required
     - A bug is suspected but cannot be reproduced in bUnit

   - Example: `NavMenuTests.cs` covers auth states (anonymous, admin, author) via
     `IAuthorizationService` mocking in bUnit. No browser needed for those flows.

2. **bUnit test structure in MyBlog**
   - Tests inherit from `BunitContext` (bUnit's `TestContext` aliased via `global using Bunit` in GlobalUsings.cs)
   - `TestAuthorizationService` helper (in `tests/Unit.Tests/Testing/`) mocks auth for testing
   - Tests use helper methods like `RenderForUser(ClaimsPrincipal)` and `CreatePrincipal()` to set up authenticated render contexts
   - Assertions use `cut.Markup.Should()` to verify rendered HTML output

   **Real example from `NavMenuTests.cs`:**
   ```csharp
   public class NavMenuTests : BunitContext
   {
       public NavMenuTests()
       {
           Services.AddAuthorizationCore();
           Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();
       }

       [Fact]
       public void UnauthenticatedUser_SeesLoginAndNoProtectedLinks()
       {
           // Arrange (none)
           // Act
           var cut = RenderForUser(CreatePrincipal(authenticated: false));

           // Assert
           cut.Markup.Should().Contain("Login");
           cut.Markup.Should().NotContain("Logout");
           cut.Markup.Should().NotContain("Manage Users");
       }

       [Fact]
       public void AuthenticatedAdmin_UsesDisplayNameAsProfileLabel_AndShowsAdminLinks()
       {
           // Arrange (none)
           // Act
           var cut = RenderForUser(CreatePrincipal(name: "Admin User", roles: ["Admin"]));

           // Assert
           cut.Markup.Should().Contain("Admin User");
           cut.Markup.Should().Contain("Manage Users");
           cut.Markup.Should().Contain("New Post");
       }
   }
   ```

   **Note:** `RenderForUser` and `CreatePrincipal` are helper methods defined in the test class itself (not in a separate base class). They wrap bUnit's `RenderComponent<T>()` with auth context setup.

3. **Run the app the same way contributors do**
   - Prefer `src/AppHost` for infrastructure-dependent checks (MongoDB, Redis).
   - Use `src/Web` only for Web-only, infrastructure-agnostic features.
   - AppHost wires all infrastructure exactly as contributors see it locally.

4. **Treat browser artifacts as debugging aids**
   - Screenshots, console logs, and network captures: Investigation only.
   - Do NOT commit browser artifacts to the repo unless explicitly asked.

5. **Escalate repeated runtime gaps into real backlog work**
   - If a browser-only regression keeps happening:
     - Add a bug report with reproduction steps
     - Open a follow-up story for a dedicated E2E/AppHost test project
     - Do NOT sneak ad-hoc browser specs into the current suite

### Good MyBlog use cases

1. **Theme color and brightness persistence** — Verify `theme.js` behavior:
   - Toggle dark mode in browser
   - Reload page
   - Assert CSS class and localStorage value persist

2. **Auth redirects and session state** — Verify end-to-end auth flows:
   - Login → redirect to `/profile` or home
   - Logout → redirect to login
   - Expired session → redirect to login
   - Run against locally running AppHost for full OAuth flow

3. **Smoke check authenticated vs. anonymous navigation** — After auth/claim changes:
   - Anonymous user → see "Login" button in NavMenu
   - Authenticated user → see "Profile" button + display role badges
   - Admin user → see "Admin" section if it exists

4. **Confirm full AppHost wiring** — Before deploying major infrastructure changes:
   - MongoDB connection → can create/read blog posts
   - Redis cache → verify cache hits don't re-fetch from DB
   - Auth0 integration → redirect chains work end-to-end

### Example: When to write a browser test

**Scenario:** NavMenu theme toggle button is not updating the CSS class in production.

**Steps:**
1. ✅ bUnit test passes: `NavMenu_TogglesThemeClass_WhenButtonClicked()`
2. ✅ Component renders correctly in bUnit
3. ❌ Local browser test fails: CSS class not applied after button click

**Next steps:**
- Investigate: Is `theme.js` actually being loaded?
- Are there console errors blocking the script?
- Open a browser test to capture the issue, then fix the bug
- Do NOT leave the browser test in the suite; replace it with bUnit coverage once the bug is fixed

### Real MyBlog Test Structure

**bUnit Component Test** (`tests/Unit.Tests/Components/Layout/NavMenuTests.cs`):
```csharp
public class NavMenuTests : BunitContext
{
    public NavMenuTests()
    {
        Services.AddAuthorizationCore();
        Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();
    }

    [Fact]
    public void UnauthenticatedUser_SeesLoginAndNoProtectedLinks()
    {
        // Arrange (none)
        // Act
        var cut = RenderForUser(CreatePrincipal(authenticated: false));

        // Assert
        cut.Markup.Should().Contain("Login");
        cut.Markup.Should().NotContain("Logout");
        cut.Markup.Should().NotContain("Manage Users");
        cut.Markup.Should().NotContain("New Post");
    }

    [Fact]
    public void AuthenticatedAdmin_UsesDisplayNameAsProfileLabel_AndShowsAdminLinks()
    {
        // Arrange (none)
        // Act
        var cut = RenderForUser(CreatePrincipal(name: "Admin User", roles: ["Admin"]));

        // Assert
        cut.Markup.Should().Contain("Admin User");
        cut.Markup.Should().Contain("Manage Users");
        cut.Markup.Should().Contain("New Post");
        cut.Markup.Should().Contain("Logout");
    }
}
```

**Pattern notes:**
- Tests inherit from `BunitContext` and set up auth via `Services.AddSingleton<IAuthorizationService, TestAuthorizationService>()`
- `RenderForUser()` and `CreatePrincipal()` are helper methods that the test class defines (or inherits)
- Assertions use `cut.Markup.Should()` to verify rendered HTML output directly

### Explicit Rejections

- **Rejected:** Automatic Playwright or Node.js installation. MyBlog has no
  browser-test project. If that changes in future, adopt Playwright via a
  dedicated `tests/Web.Tests.E2E` project with its own tooling.

- **Rejected:** Treating browser automation as a replacement for bUnit in
  `tests/Unit.Tests`. bUnit is faster, more reliable, and doesn't require a
  running server.

- **Rejected:** Adding committed Playwright specs, `package.json`, or CI
  browser-test jobs today. Future work only if the team approves a dedicated
  browser-test lane (outside current scope).

- **Rejected:** Carrying over generic form-flow or responsive-design checklists.
  MyBlog should add those only when a concrete feature needs them.

- **Rejected:** Using the commented Aspire sample in
  `tests/Integration.Tests/IntegrationTest1.cs` as canonical coverage. (TODO:
  Delete or replace with real AppHost smoke test.)
