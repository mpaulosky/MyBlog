# Contributing to MyBlog

Thank you for your interest in contributing to MyBlog — a learning project for .NET development!

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Before You Start](#before-you-start)
- [Quick Start](#quick-start)
- [Project Structure](#project-structure)
- [Design Decisions](#design-decisions)
- [How to Contribute](#how-to-contribute)
- [Testing Requirements](#testing-requirements)
- [Code Style](#code-style)

## Welcome

MyBlog is a **training project**. We welcome contributions that:
- Keep the project focused on learning (no production complexity)
- Follow clean architecture principles
- Include tests for all new functionality
- Have clear, descriptive commit messages

## Code of Conduct

We have adopted the Contributor Covenant. Contributors are expected to adhere to this code. Please report unwanted behavior to [@mpaulosky](mailto:matthew.paulosky@outlook.com).

## Before You Start

This is a learning project for practicing:
- .NET Aspire orchestration
- Blazor Server rendering
- Clean architecture (Domain/Web layer separation)
- Test-driven development with xUnit, FluentAssertions, NetArchTest.Rules

**Key principle**: Keep it simple. We use an in-memory repository by design — no database, no authentication, no external services.

## Quick Start

1. **Fork** the repository: https://github.com/mpaulosky/MyBlog/fork
2. **Clone** your fork and create a feature branch:
   ```bash
   git clone https://github.com/<your-username>/MyBlog.git
   cd MyBlog
   git checkout -b feature/your-feature-name
   ```
3. **Make your changes** following the code style guidelines
4. **Add or update tests** (required for all code changes)
5. **Run the full test suite**:
   ```bash
   dotnet test
   ```
6. **Commit** with clear messages (present tense, reference issues if applicable):
   ```bash
   git commit -m "Add blog post filtering feature"
   ```
7. **Push** and open a Pull Request to `main`

## Project Structure

```
MyBlog/
├── src/
│   ├── AppHost/              # .NET Aspire orchestration entry point
│   ├── Domain/               # BlogPost entity, repository interfaces, in-memory implementation
│   ├── ServiceDefaults/      # Aspire shared configuration (OpenTelemetry, health checks)
│   └── Web/                  # Blazor Server application
│       └── Components/
│           ├── Pages/BlogPosts/  # Index, Create, Edit Razor pages
│           ├── Pages/            # Home, Error, NotFound
│           ├── Shared/           # ConfirmDeleteDialog
│           └── Layout/           # MainLayout, NavMenu, ReconnectModal
├── tests/
│   ├── Unit.Tests/           # Entity and repository unit tests
│   ├── Architecture.Tests/    # Layer dependency validation
│   └── Integration.Tests/     # Stubbed for future Aspire integration
├── docs/                     # Documentation
├── Directory.Build.props     # Centralized build settings
├── global.json               # .NET SDK version lock
└── MyBlog.slnx               # Solution file
```

## Design Decisions

This project adheres to these core principles:

1. **Blazor Server** for interactive server rendering — simplest way to learn Aspire + dynamic UI together
2. **In-memory repository only** — training project, no database setup required
3. **.NET Aspire orchestration** — learn service composition and health checks
4. **Clean architecture** — Domain and Web layers clearly separated
5. **Repository pattern** — `IBlogPostRepository` interface with in-memory implementation
6. **Repository naming** (`MyBlog` context) vs project names** — `AppHost`, `Domain`, `Web` have no `MyBlog.` prefix (intentional: repo name provides context)

If you have architectural suggestions, please open an issue to discuss first.

## How to Contribute

### Report a Bug

[Create an issue](https://github.com/mpaulosky/MyBlog/issues):
- Use the **Bug** label
- Include steps to reproduce, expected behavior, and actual behavior
- Attach screenshots if helpful

### Suggest an Enhancement

[Create an issue](https://github.com/mpaulosky/MyBlog/issues):
- Use the **Enhancement** label
- Explain the use case and why it aligns with the project's learning goals

### Write Code

1. Link your work to an open issue (create one if needed)
2. Follow the [Testing Requirements](#testing-requirements)
3. Follow the [Code Style](#code-style) guidelines
4. Keep changes focused and clear

### Write Documentation

Help keep `/docs` and [README.md](../README.md) accurate:
- Update docs if you change architecture or features
- Add architecture decision records (ADRs) for significant changes
- Link documentation from the main README

## Testing Requirements

**All new code must include tests. Pull requests without tests will be delayed.**

### Unit Tests

- Add tests in `tests/Unit.Tests/`
- Use **xUnit** for test framework
- Use **FluentAssertions** for assertions (e.g., `result.Should().BeTrue()`)
- Use **NSubstitute** for mocks
- Follow the **AAA pattern**: Arrange / Act / Assert

Example:
```csharp
[Fact]
public void Create_WithValidTitle_ReturnsNewBlogPost()
{
    // Arrange
    var title = "My First Post";
    var content = "Content here";
    var author = "Me";

    // Act
    var post = BlogPost.Create(title, content, author);

    // Assert
    post.Title.Should().Be(title);
    post.IsPublished.Should().BeFalse();
}
```

### Architecture Tests

- Use **NetArchTest.Rules** to verify layer dependencies
- Ensure Domain does not reference Web
- Ensure tests don't reference implementation details unnecessarily

Run all tests before pushing:
```bash
dotnet test
```

## Code Style

- **Namespaces**: Follow the RootNamespace pattern (e.g., `MyBlog.Domain`, `MyBlog.Web`)
- **Formatting**: Follow C# conventions (use `.editorconfig` if configured)
- **Comments**: Only comment complex logic; clean code is self-documenting
- **Methods**: Prefer small, focused methods with clear names
- **Async/Await**: Use `async` for I/O operations; use `.Result` is discouraged
- **Short project names**: No `MyBlog.` prefix on folder/project names; repo context provides clarity

## PR Review Checklist

Before opening a PR:
- [ ] All tests pass: `dotnet test`
- [ ] Code follows style guidelines
- [ ] New features have unit tests
- [ ] Documentation updated if applicable
- [ ] Commit messages are clear and present-tense
- [ ] No unrelated changes included

---

Thank you for contributing to MyBlog! Questions? Open an issue or reach out to [@mpaulosky](https://github.com/mpaulosky).
