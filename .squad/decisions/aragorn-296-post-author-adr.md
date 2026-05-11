### 2026-05-11T18:48:17Z: Architecture — PostAuthor value object for #296

**By:** Aragorn
**Issue:** #296

---

## Decision

Replace the `string Author` field on `BlogPost` with a `PostAuthor` value object.
The `CreateBlogPostCommand` will carry a `PostAuthor` object built in the Blazor component
from the authenticated user's claims — **the handler remains clean and does not touch
`IHttpContextAccessor`** (which is unreliable after the initial HTTP handshake in Blazor Server
interactive SignalR mode).

Author is **immutable after creation**. The edit flow does not need to touch Author.

Access-control enforcement ("Authors can only edit their own posts") is **out of scope for
this sprint** and must be a new issue.

---

## PostAuthor Value Object

```csharp
// src/Domain/ValueObjects/PostAuthor.cs
namespace MyBlog.Domain.ValueObjects;

public sealed record PostAuthor(
    string Id,
    string Name,
    string Email,
    IReadOnlyList<string> Roles);
```

- Namespace: `MyBlog.Domain.ValueObjects` (new subdirectory under `src/Domain/`)
- Immutable record — no setters
- `Roles` carries whatever roles `RoleClaimsHelper.GetRoles(user)` returns at creation time
- `Id` = Auth0 `sub` claim; `Name` = `name` claim; `Email` = `email` claim

---

## Domain Change: BlogPost.cs

**Before:**

```csharp
public string Author { get; private set; } = string.Empty;
public static BlogPost Create(string title, string content, string author) { ... }
```

**After:**

```csharp
public PostAuthor Author { get; private set; } = default!;
public static BlogPost Create(string title, string content, PostAuthor author) { ... }
```

`BlogPost.Create` guard:

```csharp
ArgumentNullException.ThrowIfNull(author);
ArgumentException.ThrowIfNullOrWhiteSpace(author.Name);
```

`BlogPost.Update()` is unchanged — Author is not a parameter and is never mutated after creation.

**MongoDB storage:** The `PostAuthor` object is stored as an **embedded document** inside the
`blogposts` collection document:

```json
{
  "_id": "...",
  "Title": "...",
  "Author": {
    "Id": "auth0|abc123",
    "Name": "Jane Doe",
    "Email": "jane@example.com",
    "Roles": ["Author"]
  }
}
```

---

## BlogDbContext Change

`OnModelCreating` must declare the owned type:

```csharp
entity.OwnsOne(p => p.Author, a =>
{
    a.Property(x => x.Id);
    a.Property(x => x.Name);
    a.Property(x => x.Email);
    a.Property(x => x.Roles);
});
```

MongoDB EF Core provider supports primitive collection properties on owned types.

---

## BlogPostDto Change

Add flat author fields (avoids a nested DTO for read-only display purposes):

```csharp
internal sealed record BlogPostDto(
    Guid Id,
    string Title,
    string Content,
    string AuthorId,
    string AuthorName,
    string AuthorEmail,
    IReadOnlyList<string> AuthorRoles,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsPublished);
```

**Rationale for flat fields:** The DTO is consumed by the UI for display only. Flat fields are
simpler to bind in Razor without a nested null-check. The `Author` string property is
**removed** — all call sites must be updated.

**BlogPostMappings.cs update:**

```csharp
internal static BlogPostDto ToDto(this BlogPost post) => new(
    post.Id, post.Title, post.Content,
    post.Author.Id, post.Author.Name, post.Author.Email, post.Author.Roles,
    post.CreatedAt, post.UpdatedAt, post.IsPublished);
```

---

## MongoDB Schema: Breaking Change Assessment

⚠️ **Breaking change.** Existing documents store `Author` as a plain string:

```json
{ "Author": "Test Author" }
```

Once `BlogPost.Author` becomes a `PostAuthor` owned entity, the EF Core MongoDB provider will
attempt to deserialize the string field as an embedded document and throw.

**Mitigation for Sprint 19 (dev/test environment):**

1. Drop and recreate the `blogposts` collection in the local dev Atlas deployment.
2. Integration tests (`MongoDbBlogPostRepositoryTests`) already create a fresh database —
   no changes needed to test infrastructure.

**If production data exists:** A one-time migration script must be written before deployment
that reads all documents with a string `Author` field and rewrites them as embedded documents.
This is **out of scope for Sprint 19** — document in the PR and create a follow-up issue.

---

## CreateBlogPostCommand Change

**Before:**

```csharp
internal sealed record CreateBlogPostCommand(string Title, string Content, string Author)
    : IRequest<Result<Guid>>;
```

**After:**

```csharp
internal sealed record CreateBlogPostCommand(string Title, string Content, PostAuthor Author)
    : IRequest<Result<Guid>>;
```

The command carries the fully-built `PostAuthor`. The Blazor component (not the handler) is
responsible for reading auth state and constructing `PostAuthor` — keeping the handler
infrastructure-agnostic.

---

## CreateBlogPostCommandValidator Change

**Remove** the `Author` string validation rules (NotEmpty, MaximumLength).
**Add** a null guard rule for the `PostAuthor` object:

```csharp
RuleFor(x => x.Author).NotNull().WithMessage("Author is required.");
RuleFor(x => x.Author.Name).NotEmpty().WithMessage("Author name is required.")
    .When(x => x.Author is not null);
```

---

## CreateBlogPostHandler Change

**No change to constructor or DI.** Handler stays as-is — it calls
`BlogPost.Create(title, content, author)` where `author` is now a `PostAuthor` from the command.
No `IHttpContextAccessor` needed.

```csharp
var post = BlogPost.Create(request.Title, request.Content, request.Author);
```

The handler is blissfully unaware of where the `PostAuthor` came from.

---

## Create.razor Change

1. **Remove** the `<InputText @bind-Value="_model.Author" />` input field and its form group.
2. **Remove** `Author` from `PostFormModel`.
3. **Inject** auth state:

   ```razor
   @inject AuthenticationStateProvider AuthStateProvider
   ```

4. **Build PostAuthor in HandleSubmit:**

   ```csharp
   var authState = await AuthStateProvider.GetAuthenticationStateAsync();
   var user = authState.User;
   var author = new PostAuthor(
       user.FindFirst("sub")?.Value ?? string.Empty,
       user.FindFirst("name")?.Value ?? string.Empty,
       user.FindFirst("email")?.Value ?? string.Empty,
       RoleClaimsHelper.GetRoles(user));
   var result = await Sender.Send(new CreateBlogPostCommand(_model.Title, _model.Content, author));
   ```

5. **Show read-only author name** above the form (optional UX, Legolas to judge):

   ```razor
   <p class="form-label">Author: <span class="font-semibold">@_authorName</span></p>
   ```

   Populate `_authorName` in `OnInitializedAsync`.

---

## Edit Flow Impact

- `EditBlogPostCommand` already contains only `(Guid Id, string Title, string Content)` — no Author.
- `BlogPost.Update(title, content)` does not touch Author.
- `Edit.razor` does not display Author.

**Author is immutable after creation.** No edit-flow changes are needed for this sprint.

---

## Access Control Scope

The issue description mentions: "only the Admin and Author roles can edit, but Authors can
only edit the Posts they Authored."

The current `Edit.razor` already has `@attribute [Authorize(Roles = "Author,Admin")]` which
covers the first part.

**"Authors can only edit their own posts"** (comparing `post.Author.Id` to the current user's
`sub` claim) is **NOT in scope for Sprint 19.** This requires:

- A query-time or UI-time ownership check
- Potentially a 403 response or redirect if a non-owner Author tries to edit

➡️ **Create a new GitHub issue** for Sprint 19 or 20 titled:
`[Sprint N] feat(app): restrict post editing to post author or Admin`

---

## Work Breakdown

### Sam (backend) implements

| File | Change |
|------|--------|
| `src/Domain/ValueObjects/PostAuthor.cs` | **New file** — PostAuthor record |
| `src/Domain/Entities/BlogPost.cs` | Change `Author` type from `string` to `PostAuthor`; update `Create()` signature and guards |
| `src/Web/Data/BlogDbContext.cs` | Add `OwnsOne` mapping for `PostAuthor` |
| `src/Web/Data/BlogPostDto.cs` | Replace `string Author` with flat `AuthorId`, `AuthorName`, `AuthorEmail`, `AuthorRoles` |
| `src/Web/Data/BlogPostMappings.cs` | Update `ToDto()` mapping |
| `src/Web/Features/BlogPosts/Create/CreateBlogPostCommand.cs` | Replace `string Author` with `PostAuthor Author` |
| `src/Web/Features/BlogPosts/Create/CreateBlogPostCommandValidator.cs` | Replace string rules with null/name guard |
| `src/Web/Features/BlogPosts/Create/CreateBlogPostHandler.cs` | No logic change; compiles via type change |

### Legolas (UI) implements

| File | Change |
|------|--------|
| `src/Web/Features/BlogPosts/Create/Create.razor` | Remove Author input; inject `AuthenticationStateProvider`; build `PostAuthor` from claims in `HandleSubmit`; show read-only author name display |
| `src/Web/Features/BlogPosts/List/Index.razor` | Update any `dto.Author` string references to `dto.AuthorName` |
| `src/Web/Features/BlogPosts/Edit/Edit.razor` | Update any `dto.Author` string references (if displayed) |

### Gimli (tests) implements

| File | Change |
|------|--------|
| `tests/Domain.Tests/Entities/BlogPostTests.cs` | Update all `BlogPost.Create()` calls to pass a `PostAuthor` instead of string |
| `tests/Web.Tests/Handlers/CreateBlogPostHandlerTests.cs` | Update `CreateBlogPostCommand` construction to use a `PostAuthor` |
| `tests/Web.Tests/Features/BlogPosts/Commands/CreateBlogPostCommandValidatorTests.cs` | Rewrite Author validation tests for new rule |
| `tests/Web.Tests/Features/BlogPosts/Commands/CreateBlogPostDomainCommandValidatorTests.cs` | Same — update Author field |
| `tests/Web.Tests/Data/BlogPostMappingsTests.cs` | Update DTO field assertions (`AuthorName` etc.) |
| `tests/Web.Tests.Integration/BlogPosts/MongoDbBlogPostRepositoryTests.cs` | Update any `BlogPost.Create()` calls with `PostAuthor` |

---

## Branch

`squad/296-post-author-value-object`
