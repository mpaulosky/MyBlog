## Summary

<!-- Describe what this PR does and why. Link the issue it closes. -->

Closes #<!-- issue number -->

## Type of Change

<!-- Check all that apply -->

- [ ] 🐛 Bug fix (non-breaking change that fixes an issue)
- [ ] ✨ Feature (non-breaking change that adds functionality)
- [ ] ♻️ Refactor (no behavior change, code cleanup/restructure)
- [ ] 🧪 Tests (new or updated tests only)
- [ ] 📝 Docs (README, XML docs, comments)
- [ ] ⚙️ Infra/CI (GitHub Actions, Aspire, NuGet, deployment)
- [ ] 🔒 Security (auth, permissions, secrets, headers)
- [ ] 💥 Breaking change (existing behavior changes)

## Domain Affected

<!-- Check all that apply — this determines which reviewers are required -->

- [ ] 🏗️ Architecture / domain logic / CQRS → **Aragorn required**
- [ ] 🔧 Backend (handlers, repositories, API endpoints, MediatR) → **Sam required**
- [ ] ⚛️ Frontend (Blazor components, Razor pages, CSS, JS) → **Legolas required**
- [ ] 🧪 Unit / bUnit / integration tests → **Gimli required**
- [ ] 🧪 E2E / Playwright / Aspire integration tests → **Pippin required**
- [ ] ⚙️ CI/CD / GitHub Actions / NuGet / Aspire AppHost → **Boromir required**
- [ ] 🔒 Auth0 / authorization / security-relevant changes → **Gandalf required**
- [ ] 📝 Docs / README / XML docs → **Frodo required**

## Self-Review Checklist

<!-- Complete before requesting review — incomplete PRs will be returned -->

### Code Quality
- [ ] I ran `dotnet build MyBlog.slnx --configuration Release` — 0 errors, 0 warnings
- [ ] I ran `dotnet test MyBlog.slnx --configuration Release --no-build` — all pass
- [ ] No TODO/FIXME left unless tracked in a follow-up issue (link it)
- [ ] No secrets, API keys, or credentials committed

### Architecture
- [ ] New handlers follow the `Command`/`Query`/`Handler`/`Validator` naming conventions
- [ ] New handlers are `sealed`
- [ ] Domain layer has no references to `Web` or `Persistence.*` projects
- [ ] `Result<T>` / `ResultErrorCode` used for expected failures (no exception-driven control flow)
- [ ] DTOs are records in `Domain.DTOs`; Models are in `Domain.Models`
- [ ] No DTO types embedded in Model classes

### Tests
- [ ] New code has corresponding unit tests
- [ ] Integration tests use domain-specific collections (`[Collection("XxxIntegration")]`)
- [ ] No test compares two `IssueDto.Empty` / `CommentDto.Empty` instances directly

### Security (check if security-relevant)
- [ ] New endpoints have appropriate `RequireAuthorization` / policy applied
- [ ] No `MarkupString` used with user-supplied content
- [ ] No user input reflected in MongoDB queries without sanitization

### Merge Readiness
- [ ] Branch is up to date with `main` (no merge conflicts)
- [ ] CI checks are green (do not request review while checks are pending/failing)
- [ ] PR description is complete — reviewers should not have to ask what this does

## Screenshots / Evidence

<!-- For UI changes: before/after screenshots. For fixes: evidence the bug is resolved. -->

## Notes for Reviewers

<!-- Anything you want reviewers to pay special attention to, or context they need. -->
