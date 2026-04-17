# Pippin — History

## Learnings

### MyBlog Project Context (2025)

**Files Updated/Created:**
- `/README.md` — Complete rewrite: Changed from NewProject (issue tracker) to MyBlog (training blog)
- `/docs/CONTRIBUTING.md` — Updated all placeholders with MyBlog-specific URLs, tech stack, design principles
- `/docs/ARCHITECTURE.md` — Created new: 800+ lines covering solution structure, layers, entity design, testing strategy
- `/.squad/agents/pippin/charter.md` — Fixed: Changed role from "Tester (E2E)" to "Docs"

**Key Facts About MyBlog:**
- Training/learning project, not production. Stack: .NET 10, Aspire 13.2.2, Blazor Server (ISR)
- No database/auth/cache by design — InMemoryBlogPostRepository only
- 9 tests total: 7 unit tests (BlogPost, repository), 2 architecture tests (NetArchTest.Rules)
- Domain model: `BlogPost` entity with factory method `Create()`, mutable methods `Update()`, `Publish()`, `Unpublish()`
- Clean architecture: Web → Domain dependency, Domain has no external deps
- Short project names (AppHost, Domain, Web, ServiceDefaults) — no MyBlog. prefix; repo name provides context
- Tests all passing; uses xUnit, FluentAssertions, NSubstitute
- Blazor Server components: Index, Create, Edit pages + ConfirmDeleteDialog

**Documentation Principle:**
- Keep docs accurate and scoped to what project actually is
- Never add features/tech that don't exist in codebase
- Links point to real repo (mpaulosky/MyBlog), not template repos
