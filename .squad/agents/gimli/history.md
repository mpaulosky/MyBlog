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
