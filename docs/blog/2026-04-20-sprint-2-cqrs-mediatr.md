---
title: "Sprint 2: CQRS and MediatR Deep Dive"
date: 2026-04-20
author: Bilbo
tags: [cqrs, mediatr, testing, domain, sprint-2]
summary: "Sprint 2 establishes command-query separation and MediatR handlers for clean, testable domain logic."
---

## Summary

Sprint 2 brings the CQRS (Command Query Responsibility Segregation) pattern to life with five fully-tested MediatR handlers for BlogPost operations. We added FluentValidation integration through a ValidationBehavior pipeline, restructured Domain as vertical slices, and established the squad governance workflow. The test suite grew by 13 new tests, achieving ≥89% coverage. Release v1.0.0-sprint2 marks our first semantic versioning milestone.

## Context

CQRS separates reads from writes, making our code easier to reason about and optimize independently. MediatR provides the dispatch mechanism. Combined with FluentValidation, we get:
- **Clear intent**: Commands for mutations, Queries for reads
- **Automatic validation**: Pipeline behaviors intercept and validate before handlers execute
- **Testability**: Each handler can be tested in isolation
- **Scalability**: Later, we can add caching, logging, or authorization as behaviors without touching handlers

This is the backbone of maintainable domain logic.

## Key Details

### Five MediatR Handlers
```
Domain/Features/BlogPosts/
├── Commands/
│   ├── CreateBlogPostCommand.cs
│   ├── UpdateBlogPostCommand.cs
│   └── DeleteBlogPostCommand.cs
├── Queries/
│   ├── GetAllBlogPostsQuery.cs
│   └── GetBlogPostByIdQuery.cs
└── Handlers/
    ├── CreateBlogPostCommandHandler.cs
    ├── UpdateBlogPostCommandHandler.cs
    ├── DeleteBlogPostCommandHandler.cs
    ├── GetAllBlogPostsQueryHandler.cs
    └── GetBlogPostByIdQueryHandler.cs
```

### ValidationBehavior Pipeline
```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure != null)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}
```

### Test Expansion
- **13 new tests** added for command and query handlers
- Validation tests ensure bad data is caught before persistence
- Handler tests verify correct output and state changes

### Squad Governance Milestone
We established the **no-code-before-issue** gate:
- Every feature starts as a GitHub issue
- Squad members label and discuss design
- PRs must reference the issue
- Reviews ensure quality before merge

**Related PRs:** #52, #53, #54, #55, #57, #58

## What's Next

Sprint 3 adds end-to-end testing with xUnit and .NET Aspire, plus CI hardening for the squad workflow.

---

*Posted by Bilbo • Sprint 2 | Release: v1.0.0-sprint2*
