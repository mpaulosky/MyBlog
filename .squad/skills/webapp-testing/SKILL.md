---
name: webapp-testing
description: >
  MyBlog guidance for running-browser verification of the Blazor UI after bUnit
  coverage exists, especially for JS interop, Auth0 redirects, and AppHost smoke
  checks.
---

## MyBlog Web Application Testing

### Current repo fit

- Automated UI coverage currently lives in `tests/Unit.Tests` with bUnit:
  `NavMenuTests`, `ProfileTests`, and `RazorSmokeTests`.
- There is **no** Playwright or other browser-test project in the repo today.
- Browser-level checks are still useful for runtime-only behavior such as:
  - `src/Web/wwwroot/js/theme.js` interactions
  - `/Account/Login` and `/Account/Logout` redirect wiring in `src/Web/Program.cs`
  - AppHost smoke flows where MongoDB and Redis wiring matter

### Retained MyBlog guidance

1. **bUnit first, browser second**
   - Add or update automated coverage in `tests/Unit.Tests` before reaching for a
     real browser.
   - Use browser automation only when JS interop, navigation, console errors, or
     full runtime behavior cannot be trusted from bUnit alone.

2. **Run the app the same way contributors do**
   - Prefer launching the app through `src/AppHost` so Aspire wires MongoDB,
     Redis, and the Web project exactly as contributors see them locally.
   - If the check is Web-only and infrastructure is irrelevant, `src/Web` is an
     acceptable shortcut.

3. **Treat browser artifacts as debugging aids**
   - Screenshots, console logs, and network captures are for investigation.
   - Do not commit those artifacts to the repo unless a task explicitly asks for
     them.

4. **Escalate repeated runtime gaps into real backlog work**
   - If a browser-only regression keeps happening, open a follow-up for a future
     dedicated E2E/AppHost test project instead of sneaking ad-hoc browser specs
     into the current suite.

### Good MyBlog use cases

- Verify theme color and brightness persistence after touching `NavMenu.razor` or
  `theme.js`.
- Smoke-check authenticated versus anonymous navigation after auth or claim
  changes.
- Confirm login/logout redirect behavior against a locally running app.
- Reproduce runtime issues before replacing or removing the generic
  `IntegrationTest1.cs` scaffold.

### Explicit rejections

- **Rejected:** Automatic Playwright or Node.js installation as a default repo
  convention. MyBlog does not have a browser-test project to receive that tooling.
- **Rejected:** Treating browser automation as a replacement for bUnit in
  `tests/Unit.Tests`.
- **Rejected:** Adding committed Playwright specs, `package.json`, or CI browser
  jobs as part of this adaptation. That is future work only if the team approves a
  dedicated browser-test lane.
- **Rejected:** Carrying over generic form-flow or responsive-design checklists as
  default guidance. MyBlog should add those only when a concrete feature needs
  them.
- **Rejected:** Treating the commented Aspire sample in
  `tests/Integration.Tests/IntegrationTest1.cs` as canonical coverage.
