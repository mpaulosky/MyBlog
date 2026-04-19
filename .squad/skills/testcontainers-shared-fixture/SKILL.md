---
name: testcontainers-shared-fixture
confidence: high
description: >
  MyBlog integration-test convention for sharing one MongoDbFixture per xUnit
  domain collection in tests/Integration.Tests while keeping per-test database
  isolation.
---

## MyBlog Testcontainers Shared Fixture

### Current repo fit

- `tests/Integration.Tests/Infrastructure/MongoDbFixture.cs` already owns the
  Mongo Testcontainers lifecycle.
- `tests/Integration.Tests/BlogPosts/MongoDbBlogPostRepositoryTests.cs` is the
  live Mongo-backed integration suite today.
- The repo currently has one Mongo-backed domain collection, so the pattern is
  about keeping future tests aligned, not chasing the original import's
  large-suite performance numbers.

### Retained MyBlog conventions

1. **Use one shared Mongo fixture per xUnit collection**
   - Define domain collections with
     `ICollectionFixture<MongoDbFixture>`.
   - Use names like `BlogPostIntegration`, `AuthorIntegration`, or
     `CommentIntegration`.
   - Do **not** use the old generic `"MongoDb"` collection name for new work.

2. **Keep per-test isolation with a unique database name**
   - Generate database names with `$"T{Guid.NewGuid():N}"`.
   - xUnit creates a fresh test-class instance per test method, so constructor or
     helper-level database creation keeps tests isolated inside the shared
     container.

3. **Keep fixture responsibilities narrow**
   - `MongoDbFixture` starts and disposes the container.
   - `MongoDbFixture.CreateFactory(dbName)` is the canonical way to create a
     `BlogDbContext` factory for tests.
   - Shared seed data or cleanup logic does **not** belong in the fixture unless
     every test in that collection truly needs it.

4. **Use collection-level parallelism only**
   - Keep `parallelizeAssembly: false`.
   - Allow `parallelizeTestCollections: true` so different domain collections can
     run in parallel once they exist.

### Canonical MyBlog shape

```csharp
[CollectionDefinition("BlogPostIntegration")]
public sealed class BlogPostIntegrationCollection
		: ICollectionFixture<MongoDbFixture> { }

[Collection("BlogPostIntegration")]
public sealed class MongoDbBlogPostRepositoryTests(MongoDbFixture fixture)
{
	private MongoDbBlogPostRepository CreateRepo(string? dbName = null) =>
			new(fixture.CreateFactory(dbName ?? $"T{Guid.NewGuid():N}"));
}
```

### Next-step guidance

- Keep all Mongo-backed repository and handler integration tests for blog posts in
  `BlogPostIntegration`.
- When another Mongo-backed domain appears, add a new collection definition for
  that domain instead of creating another container fixture type.
- Replace the commented Aspire scaffold in
  `tests/Integration.Tests/IntegrationTest1.cs` with a real AppHost smoke test
  or delete it during cleanup; it is not part of this fixture pattern.

### Explicit rejections

- **Rejected:** Imported `CategoryIntegration`, `IssueIntegration`,
  `CommentIntegration`, and `StatusIntegration` mappings from the source repo.
  MyBlog only has live `BlogPost` integration coverage today.
- **Rejected:** Imported "23 classes / 46 seconds to 4 containers / 2 seconds"
  performance claims. They do not describe MyBlog's current suite and should not
  be repeated as if they were measured here.
- **Rejected:** Adding `GlobalUsings.cs` just to avoid one namespace import. Keep
  the file only if repetition in `tests/Integration.Tests` makes it worthwhile.
- **Rejected:** Putting `IAsyncLifetime` on integration test classes unless the
  class has extra async setup beyond the shared Mongo fixture.
