# Sam's Work History

## 2026-04-17: Optimistic Concurrency Implementation

### Task: Implement Optimistic Concurrency — Handlers + UI

Added `DbUpdateConcurrencyException` handling to Edit and Delete MediatR handlers, working alongside Aragorn who is implementing the database-layer concurrency token configuration.

### Changes Made
1. **EditBlogPostHandler.cs**: Added catch block for `DbUpdateConcurrencyException` returning `Result.Fail` with `ResultErrorCode.Concurrency`
2. **DeleteBlogPostHandler.cs**: Added catch block for `DbUpdateConcurrencyException` returning `Result.Fail` with `ResultErrorCode.Concurrency`
3. **Edit.razor**: Added UI to display concurrency warning alert when error code indicates concurrency conflict
4. **Index.razor**: Added UI to display concurrency warning alert in delete flow when error code indicates concurrency conflict

### Technical Decisions
- Used `global::Domain.Abstractions.ResultErrorCode.Concurrency` fully qualified name in Razor files to avoid conflicts with the Web project's RootNamespace setting (`MyBlog.Web`)
- Positioned concurrency catch before generic Exception catch to ensure specific handling
- Displayed user-friendly messages explaining the conflict and suggesting to reload

### Build Validation
- ✅ `dotnet build src/Web/Web.csproj` succeeded with 0 errors, 0 warnings

## Learnings

### Namespace Resolution in Blazor Razor Components
- When a project has a `<RootNamespace>` setting (e.g., `MyBlog.Web`), the Blazor compiler prepends that namespace to relative `@using` directives in `.razor` files
- Using `@using Domain.Abstractions` in a Razor file within the Web project tries to resolve to `MyBlog.Web.Domain.Abstractions`, not `Domain.Abstractions`
- Solution: Use fully qualified names with `global::` prefix when referencing external namespaces: `global::Domain.Abstractions.ResultErrorCode`
- This is specific to Blazor Razor compilation; regular C# files in the same project don't have this issue with `using Domain.Abstractions;`

### DbUpdateConcurrencyException Handling
- `Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException` is thrown when SaveChanges detects a concurrency conflict
- Best practice: Catch this specific exception before catching generic `Exception` to provide targeted error handling
- Return domain-appropriate Result with error code for UI to handle gracefully

### Result Pattern + Error Codes
- `Result.Fail(message, ResultErrorCode)` allows structured error handling
- UI can check `result.ErrorCode` to display context-specific messages
- Keeps business logic concerns (concurrency) separate from presentation (warning vs. error styling)
