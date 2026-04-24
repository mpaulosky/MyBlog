---
title: "MyBlog Project Kickoff: Building with .NET 10 and Clean Architecture"
date: 2026-04-18
author: Bilbo
tags: [aspire, blazor, clean-architecture, sprint-1]
summary: "The MyBlog project launches with a solid foundation: Blazor Server, .NET Aspire, and clean architecture—ready for modern web development."
---

## Summary

MyBlog begins with a strong architectural foundation. Built on the Blazor Server template, the project spans five well-organized projects: AppHost for orchestration, Domain for business logic, ServiceDefaults for shared infrastructure, and Web for the UI layer. Sprint 1 focused on removing template clutter, adding proper copyright headers, and establishing the first data model with in-memory persistence. The suite passes 74 tests across Architecture, Unit, and Integration layers.

## Context

When starting a new project, the first decisions matter. We chose:
- **Blazor Server** for rapid, interactive web development with C#
- **.NET 10** and **.NET Aspire 13.2.2** for modern, cloud-native capabilities
- **Clean architecture** with vertical slices to keep code organized and testable as the project grows
- **In-memory persistence** initially to keep things simple while we establish patterns

This foundation lets us move fast without technical debt.

## Key Details

### Project Structure
```
MyBlog/
├── AppHost/              # .NET Aspire orchestration
├── Domain/               # Business logic + entities
├── ServiceDefaults/      # Shared configs
├── Web/                  # Blazor Server UI
└── Tests/
    ├── Architecture.Tests/
    ├── Unit.Tests/
    └── Integration.Tests/
```

### Early PRs
- **PR #6:** Removed demo Blazor components (Counter, Weather)
- **PR #7:** Added MIT license headers to all C# files

### Data Model
The `BlogPost` entity is simple but functional:
```csharp
public class BlogPost
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

With an in-memory repository backing the initial feature set.

### Test Coverage
- **74 tests passing** across three test projects
- Architecture tests enforce layer dependencies
- Unit tests cover business logic
- Integration tests verify the persistence layer

## What's Next

Sprint 2 introduces CQRS and MediatR to establish a robust command/query pattern. The in-memory repository will be joined by validation handlers and a proper vertical slice structure.

---

*Posted by Bilbo • Sprint 1 Foundation*
