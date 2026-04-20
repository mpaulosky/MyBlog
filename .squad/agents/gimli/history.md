## Session: VSA Handler Test Coverage (2025)

### Task
Write comprehensive handler unit tests for MyBlog after upgrade to Vertical Slice Architecture with MediatR, MongoDB, Redis/IMemoryCache, and Auth0.

### Work Done
- **Task 1**: InMemoryBlogPostRepository was already deleted; no action needed.
- **Task 2**: Updated `Unit.Tests.csproj` — added `<ProjectReference>` to `src/Web/Web.csproj`; removed explicit `Microsoft.Extensions.Caching.*` package refs that caused NU1605 downgrade conflicts (types come transitively).
- **Task 3**: Preserved `BlogPostTests.cs` unchanged.
- **Task 4**: Created handler unit tests (xUnit + FluentAssertions + NSubstitute):
  - `Handlers/GetBlogPostsHandlerTests.cs` — 4 tests (L1 hit, L2 hit, full cache miss, repo throws)
  - `Handlers/CreateBlogPostHandlerTests.cs` — 2 tests (success, repo throws)
  - `Handlers/DeleteBlogPostHandlerTests.cs` — 2 tests (success, not found)
  - `Handlers/EditBlogPostHandlerTests.cs` — 5 tests (edit success, edit not found, getById L1 hit, getById null, getById posts)
- **Task 5**: Architecture tests updated — added `Domain_Should_Not_Have_InMemoryRepository` (using `GetTypes().Should().BeEmpty()`); created `VsaLayerTests.cs` with 3 VSA structure tests.
- **Task 6**: All tests pass — 20 unit tests, 6 architecture tests (0 failures).

### Key Learnings — NSubstitute + IMemoryCache

1. **`IMemoryCache.Set<T>` is an extension method** — NSubstitute cannot intercept it. `Set<T>` internally calls `cache.CreateEntry(key)` which IS a real interface method. Mock and verify `CreateEntry` instead.
   ```csharp
   var cacheEntry = Substitute.For<ICacheEntry>();
   _localCache.CreateEntry(Arg.Any<object>()).Returns(cacheEntry);
   // verify: _localCache.Received(1).CreateEntry(Arg.Any<object>());
   ```

2. **`IMemoryCache.TryGetValue` out-param mock** — Must use `Returns(callback)` pattern with `x[1] = value` to set the out parameter:
   ```csharp
   object? outVal = null;
   _localCache.TryGetValue(Arg.Any<object>(), out outVal)
       .Returns(x => { x[1] = (object)cachedValue; return true; });
   ```
   Using `Arg.Any<object>()` for key is more permissive and avoids match failures.

3. **`IMemoryCache.Remove` IS a real interface method** — `Received()` verification works fine.

4. **`IDistributedCache.SetAsync/GetAsync` ARE real interface methods** — `Received()` verification works fine.

5. **NetArchTest.Rules 1.3.2** — `.ShouldNot().Exist()` does not exist. Use `GetTypes().Should().BeEmpty()` with FluentAssertions instead.

6. **`IBaseRequestHandler` is not a real MediatR type** — don't use it in architecture tests. `BeSealed()` is a valid architectural constraint since all handlers in this project are `sealed class`.

## Session: PR #2 Test Review (2025-04)

### Task
Review PR #2 test files for compliance with Gimli's Critical Rules and team conventions.

### Findings

**Files reviewed:**
- tests/Unit.Tests/Components/Layout/NavMenuTests.cs
- tests/Unit.Tests/Components/RazorSmokeTests.cs
- tests/Unit.Tests/Features/UserManagement/ProfileTests.cs
- tests/Unit.Tests/ResultTests.cs
- tests/Unit.Tests/Security/RoleClaimsHelperTests.cs
- tests/Unit.Tests/Testing/TestAuthorizationService.cs
- tests/Unit.Tests/Unit.Tests.csproj

**Critical Rules Violations:**
1. **MISSING FILE HEADERS** — ALL 6 test files lack the required block-format copyright header. Production code in `src/Domain/Abstractions/Result.cs` shows the expected format:
   ```csharp
   // =======================================================
   // Copyright (c) 2025. All rights reserved.
   // File Name :     Result.cs
   // Company :       mpaulosky
   // Author :        Matthew Paulosky
   // Solution Name : MyBlog
   // Project Name :  Domain
   // =======================================================
   ```

2. **MISSING AAA COMMENTS** — None of the new test files use `// Arrange`, `// Act`, `// Assert` comments. Existing handler tests in `tests/Unit.Tests/Handlers/` consistently use AAA comments per charter requirement.

**Passes:**
- ✅ FluentAssertions `.Should()` used throughout
- ✅ NSubstitute used for mocking in `TestAuthorizationService`
- ✅ File-scoped namespaces used
- ✅ Test namespace pattern correct (`MyBlog.Unit.Tests.{Folder}`)
- ✅ bUnit tests properly configured with `BunitContext` base class
- ✅ No integration test collection attributes needed (these are unit tests, not integration)
- ✅ No `{Entity}Dto.Empty` comparisons

**Coverage Strengths:**
- NavMenu: Excellent coverage of auth states (unauthenticated, admin, author, theme interaction)
- Profile: Good coverage of claim presence/absence scenarios
- RoleClaimsHelper: Comprehensive theory tests for role expansion formats
- RazorSmokeTests: Good basic component rendering tests
- ResultTests: Solid coverage of Result<T> patterns
- TestAuthorizationService: Clean test helper for bUnit auth scenarios

**Verdict:** REQUEST CHANGES — Missing file headers and AAA comments violate Critical Rules #6 and #3.

### Learnings

1. **File header enforcement** — Production code has block-format copyright headers; test code MUST match per charter rule #6. Header is mandatory, not optional.

2. **AAA pattern with comments** — Charter rule #3 explicitly requires `// Arrange`, `// Act`, `// Assert` comments. Existing handler tests demonstrate this pattern consistently.

3. **bUnit test structure** — `BunitContext` base class + test helpers (like `TestAuthorizationService`) + `RenderForUser` patterns create clean, reusable component test setup.

4. **Test namespace convention** — `MyBlog.Unit.Tests.{Folder}` matches the charter requirement for unit test namespace pattern.

## Session: Remove Weather and Counter Tests (2026-04)

### Task
Remove ALL test code related to Weather and Counter from the test projects. These are Blazor default template leftovers.

### Work Done
**Coordination:** Worked on shared branch `squad/remove-weather-counter` created by Legolas.

**Discovery:** Legolas had already completed ALL test cleanup in commit `4dc0b08`:
- Removed 2 test methods from `tests/Unit.Tests/Components/RazorSmokeTests.cs`:
  - `Counter_Increments_WhenButtonClicked()` — tested the Counter component increment behavior
  - `Weather_LoadsForecasts()` — tested Weather component forecast loading

**Verification:**
- Searched entire `tests/` directory — NO remaining Weather or Counter references found
- Architecture tests (`tests/Architecture.Tests/`) — clean
- Integration tests (`tests/Integration.Tests/`) — clean
- Unit tests (`tests/Unit.Tests/`) — only RazorSmokeTests.cs was modified

**Build & Test Results:**
✅ Build: SUCCESS (0 warnings, 0 errors)
✅ Tests: ALL PASSING
- Architecture.Tests: 6 tests passed
- Unit.Tests: 59 tests passed (down from 61 after removing 2 Weather/Counter tests)
- Integration.Tests: 9 tests passed
- Code coverage: 91.64% line coverage maintained

**PR Created:** https://github.com/mpaulosky/MyBlog/pull/6

### Learnings

1. **Team coordination** — When working on a shared branch, another team member may complete overlapping work. Always check existing commits before starting work.

2. **No Weather/Counter integration/architecture tests existed** — The template leftovers were only tested in unit tests (RazorSmokeTests.cs). No cleanup needed in Architecture.Tests or Integration.Tests.

3. **Test count tracking** — After removing template tests:
   - BEFORE: 61 unit tests (including Counter + Weather)
   - AFTER: 59 unit tests (real blog functionality only)
   - Net reduction: 2 tests removed

4. **Shared branch workflow** — When working with Legolas on the same branch:
   - Check if branch exists remotely before creating locally
   - Pull latest changes to avoid duplication
   - Review what's been done already (via `git log`, `git show`)
   - Add complementary work if needed, or verify and open PR if complete

## 2026-04-19 — Testcontainers Adoption (Skills Review)

As part of squad skills/playbooks review, testcontainers-shared-fixture pattern identified as highest-ROI optimization for integration tests.

**Scope:** Reduce startup time from ~46s (per-class) to ~2s (shared containers) via MongoDB shared fixture + xunit collection parallelization.

**Next Steps:** Map to MyBlog collections (BlogPosts, Authors, Comments, Tags, Categories); configure `xunit.runner.json` with `parallelizeAssembly: false` (collection-level only).

**Timeline:** Sprint 7 (2h estimated).

**Owner:** Gimli (Testing) — routed with `testcontainers-shared-fixture` skill.

## 2026-04-20 — Sprint 2: Testing Patterns Extraction

### Task
Extract and refine testing patterns from current MyBlog assets into cohesive,
reusable squad guidance. Improve existing skills to reflect real repo patterns
and conventions.

### Work Done

1. **Reviewed current testing SKILL assets:**
   - `testcontainers-shared-fixture/SKILL.md` — MongoDB fixture patterns
   - `webapp-testing/SKILL.md` — bUnit and browser testing guidance
   - Real test projects: `tests/Integration.Tests/`, `tests/Unit.Tests/`, `tests/Architecture.Tests/`

2. **Refined testcontainers-shared-fixture/SKILL.md:**
   - Expanded "Current repo fit" section with real measurements (~2–3s startup, acceptable for MVP)
   - Renamed "Retained MyBlog conventions" → "MyBlog Tested Patterns (Established)"
     - Added fixture responsibility isolation details (what it does, what it doesn't)
     - Clarified xUnit configuration rationale (parallelizeAssembly: false, parallelizeTestCollections: true)
   - Added "Real MyBlog Examples" section with working code from repo:
     - BlogPostIntegrationCollection.cs (collection definition)
     - MongoDbBlogPostRepositoryTests.cs (test class pattern)
     - MongoDbFixture.cs (fixture implementation with TestContextFactory)
   - Added "Next-step guidance for new domains" — how to create AuthorIntegration, CommentIntegration, etc.
   - Updated "Current Test Coverage" to list BlogPostIntegration's 9 passing tests
   - Clarified rejections (performance claims, seeding patterns, IAsyncLifetime on test classes)

3. **Refined webapp-testing/SKILL.md:**
   - Expanded "Current repo fit" with real numbers (59 unit tests, 91.64% coverage, test files listed)
   - Renamed "Retained MyBlog guidance" → "MyBlog-Tested Patterns (Established)"
   - Added "bUnit test structure in MyBlog" with patterns for BunitContext base class and RenderForUser() helper
   - Added "Real MyBlog Test Structure" section showing working code examples:
     - NavMenuTests.cs (authentication states)
     - BunitContext.cs base class with CreatePrincipal() and RenderForUser() helpers
   - Added "Example: When to write a browser test" — scenario-based guidance
   - Added more concrete "Good MyBlog use cases" (4 specific scenarios)
   - Updated rejections to reflect no Playwright installation, no browser jobs today

4. **Created NEW SKILL: unit-test-conventions/SKILL.md** ✅
   - Comprehensive guide for unit tests in `tests/Unit.Tests/`
   - Covers file headers (7-line copyright block, required by charter rule #6)
   - Test namespace pattern (`MyBlog.Unit.Tests.{Folder}`)
   - AAA pattern with comments (required by charter rule #3)
   - FluentAssertions `.Should()` everywhere (charter rule)
   - NSubstitute mocking patterns:
     - Standard substitutes pattern for handler tests
     - IMemoryCache.Set<T> gotcha (extension method → mock CreateEntry() instead)
     - IMemoryCache.TryGetValue out-param pattern
   - Domain entity tests (BlogPost pattern, no mocks, test factories)
   - Handler tests (Vertical Slice pattern, all deps mocked)
   - bUnit component tests (BunitContext base class, RenderForUser helper)
   - Critical gotchas:
     - NEVER compare two Dto.Empty calls (DateTime.UtcNow called each time)
     - GenerateSlug trailing underscore is expected, not a bug
   - Real working code examples from repo (BlogPostTests, GetBlogPostsHandlerTests, EditBlogPostHandlerTests, ProfileTests)
   - Namespace/file organization
   - Local test run command
   - Coverage goals (80–85%)

### Key Learnings

1. **MyBlog's testing stack is mature and battle-tested:**
   - Integration tests: MongoDbFixture + collection isolation working well (9 tests)
   - Unit tests: 59 tests with 91.64% line coverage, all handlers + domain + components covered
   - Architecture tests: VSA + layer rules enforced
   - bUnit component tests: Clean auth mocking pattern with TestAuthorizationService

2. **Patterns extraction requires grounding in real code:**
   - Cannot document intent without seeing actual implementations
   - Real examples > abstract rules (code > explanation)
   - Test classes show the patterns better than generic guidance

3. **Three distinct testing domains with different conventions:**
   - **Integration**: Fixture + collection + per-test isolation
   - **Unit**: AAA + mocking + assertions, split by entity/handler/component
   - **Component**: bUnit with auth context + claim assertions

4. **File headers are critical for team consistency:**
   - Every test file MUST have the 7-line block (charter rule #6)
   - This was flagged in PR #2 review — ALL test files were missing headers
   - Now documented explicitly in unit-test-conventions skill

5. **No new skills created for existing patterns:**
   - testcontainers and webapp-testing were already good patterns
   - Refinement (not creation) is the right approach for 70–80% patterns
   - Only created unit-test-conventions because it was extractable from 4+ test files and not documented

### Outcomes

**Skills improved:**
1. `testcontainers-shared-fixture/SKILL.md` — +50% more detailed, with real code + next-step guidance
2. `webapp-testing/SKILL.md` — +40% more detailed, with real code + examples
3. `unit-test-conventions/SKILL.md` — NEW, 14KB comprehensive guide

**Documentation impact:**
- Testcontainers patterns now crystal clear (collection definition, fixture, factory, per-test isolation)
- Unit test conventions documented (headers, AAA, mocking, assertions) for future contributors
- Component testing patterns visible (BunitContext, CreatePrincipal, RenderForUser)
- Real working code examples in all three skills

**Team velocity:**
- Future test authors can now reference SKILL files instead of copying patterns
- New domain collections (Author, Comment, Tag) have clear guidance
- PR reviews can cite these skills for test quality gates

### Learnings for Gimli

1. Skills extraction is about making implicit patterns explicit. Always ground guidance in real code.
2. Three-tier testing (integration / unit / component) requires three distinct SKILL docs or clear sections within one.
3. File headers matter for consistency — enforce them in reviews (charter rule #6).
4. AAA comments aren't optional — they're team convention (charter rule #3).
5. NSubstitute gotchas (IMemoryCache.Set<T>) should be called out explicitly in training.
