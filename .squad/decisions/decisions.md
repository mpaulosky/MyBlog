---
# Transactions and Concurrency Handling

**Date:** 2025-01-29  
**Architect:** Aragorn  
**Status:** Proposed  
**Context:** Training project using MongoDB via EF Core adapter with Vertical Slice Architecture

---

## Question 1: Should We Use MongoDB Transactions?

### Current State
- MongoDB accessed via `MongoDB.EntityFrameworkCore` adapter
- Single-document operations only (CRUD on BlogPost entities)
- `SaveChangesAsync()` calls are atomic at the document level (MongoDB guarantee)
- No multi-document operations or complex workflows
- Aspire local MongoDB container (standard standalone instance, **not** replica set)

### Analysis
**MongoDB transactions require a replica set.** The Aspire `.AddMongoDB("mongodb")` provisioning creates a standalone MongoDB instance, which does NOT support multi-document transactions.

**Do we need transactions for this project?**
- Current operations are single-document CRUD → inherently atomic
- No cross-document consistency requirements
- No batch operations spanning multiple documents
- Blog post operations are independent (create/update/delete one post at a time)

**Cost/Complexity vs. Benefit:**
- **To enable transactions:** Would require configuring MongoDB replica set (3+ nodes), either locally via Docker Compose or migrating to MongoDB Atlas. This adds significant infrastructure complexity.
- **Benefit:** Zero — we have no multi-document operations that require transactional atomicity.

### Decision: **NO — Do not implement transactions**

**Reasoning:**
1. MongoDB's single-document operations are already atomic
2. We have no multi-document workflows requiring transactions
3. Local dev environment doesn't support replica sets without major config changes
4. This is a training project — complexity doesn't justify theoretical future-proofing
5. If multi-document atomicity becomes required later, we can revisit

**Action:** Document this decision and move forward without transactions.

---

## Question 2: How Are We Handling Concurrency?

### Current State
**We are NOT handling concurrency at all.** Here's what I found:

1. **No concurrency tokens:** `BlogPost` entity has no version field, timestamp, or `[ConcurrencyCheck]` attribute
2. **No EF Core concurrency:** `BlogDbContext` has no concurrency configuration
3. **No exception handling:** Handlers don't catch `DbUpdateConcurrencyException`
4. **Result pattern ready:** `ResultErrorCode.Concurrency = 1` exists but is unused
5. **Last-write-wins:** If two admins edit the same post simultaneously, the last `SaveChangesAsync()` silently overwrites the first

### Concurrency Scenarios for Blog CRUD
**Realistic conflict scenarios:**
- Two admins editing the same blog post simultaneously
- Admin editing while another admin deletes the same post
- Race condition: Admin updates post while cached read serves stale data

**Risk level for training project:** Low-to-Medium
- Single-user dev environment → conflicts unlikely
- Production blog with multiple admins → conflicts possible but rare
- Impact: Lost edits (frustrating but not data-corrupting)

### Implementation Options

#### Option A: **Optimistic Concurrency (Recommended)**
Add a version field to `BlogPost`:

```csharp
// BlogPost.cs
public int Version { get; private set; }

// Increment on update
public void Update(string title, string content)
{
    Title = title;
    Content = content;
    UpdatedAt = DateTime.UtcNow;
    Version++; // Increment version
}
```

Configure in `BlogDbContext`:
```csharp
entity.Property(p => p.Version).IsConcurrencyToken();
```

Update handlers to catch and handle:
```csharp
catch (DbUpdateConcurrencyException ex)
{
    return Result.Fail("Post was modified by another user. Please reload and try again.",
        ResultErrorCode.Concurrency,
        new { ServerVersion = ex.Entries.First().GetDatabaseValues()?["Version"] });
}
```

**Pros:**
- Industry-standard pattern for web apps
- MongoDB EF Core provider supports `IsConcurrencyToken()`
- Teaches proper concurrency handling
- Minimal performance impact (one field)
- Fails fast with clear error message

**Cons:**
- Requires schema change (add Version field)
- Requires handler updates
- Requires UI to handle concurrency errors (reload + retry)

#### Option B: **Do Nothing (Acceptable for Training)**
Accept last-write-wins behavior.

**Pros:**
- Zero code changes
- Zero complexity
- Acceptable for single-developer training scenario

**Cons:**
- Silent data loss if conflicts occur
- Doesn't teach proper patterns
- Poor user experience if multiple admins exist

#### Option C: **Pessimistic Locking (Overkill)**
Use MongoDB transactions with `FindOneAndUpdate` locking.

**Pros:**
- Prevents conflicts entirely

**Cons:**
- Requires replica set (see Question 1)
- Over-engineered for blog CRUD
- Reduces throughput

---

## Recommendation

### For This Training Project: **Implement Optimistic Concurrency (Option A)**

**Rationale:**
1. **Pedagogical value:** Teaches industry-standard pattern (optimistic concurrency)
2. **Minimal complexity:** Single field + exception handling
3. **Real-world readiness:** Pattern scales to production scenarios
4. **Respects user edits:** Prevents silent data loss
5. **Already prepared:** `ResultErrorCode.Concurrency` exists

### Implementation Plan
If team approves, I will:

1. **Domain layer:**
   - Add `public int Version { get; private set; }` to `BlogPost`
   - Increment `Version++` in `Update()` method

2. **Data layer:**
   - Configure `.IsConcurrencyToken()` in `BlogDbContext.OnModelCreating()`

3. **Handler layer:**
   - Wrap `SaveChangesAsync()` calls in `try-catch` for `DbUpdateConcurrencyException`
   - Return `Result.Fail(..., ResultErrorCode.Concurrency, details)` with server version

4. **UI layer:**
   - Display concurrency error message
   - Prompt user to reload and retry
   - (Future enhancement: Show diff of conflicting changes)

5. **Testing:**
   - Write integration test simulating concurrent edits

**Estimated effort:** 2-4 hours  
**Risk:** Low (well-understood pattern)

---

## Summary

| Question | Answer | Action |
|----------|--------|--------|
| **Transactions?** | No — not needed for single-document CRUD | Document and close |
| **Concurrency?** | Currently unhandled (last-write-wins) | Implement optimistic concurrency |

**Next Steps:**
1. Team review this decision
2. If approved, create implementation tasks
3. Update handlers, entity, and context
4. Add integration tests

---

**Reviewed by:** _(pending)_  
**Approved by:** _(pending)_

---
### 2026-04-17T15:30: User directive
**By:** Matthew Paulosky (via Copilot)
**What:** Do not prefix project folders or .csproj file names with `MyBlog.` — the repo name already provides that context. Namespaces may still use `MyBlog.*` via `<RootNamespace>`.
**Why:** User request — captured for team memory

---
### 2026-04-17T15:30: User directive
**By:** Matthew Paulosky (via Copilot)
**What:** This is a training project only — not production. Do not apply production-level concerns (e.g., hardened security, HA, SLAs) unless explicitly asked.
**Why:** User request — captured for team memory

---
### 2026-04-17: User directive
**By:** mpaulosky (via Copilot)
**What:** Do not prefix project/folder names with the repo name. Projects live under `src/AppHost`, `src/Web`, `src/Domain` — not `src/MyBlog.AppHost` etc. The repo context is implicit. Namespaces may still use the `MyBlog.*` root namespace for clarity.
**Why:** User request — captured for team memory

---
# Decision: VSA Handler Test Coverage Complete

**Date**: 2025  
**Author**: Gimli (Tester)  
**Status**: Accepted

## Context

MyBlog was upgraded to Vertical Slice Architecture (VSA) with MediatR handlers, MongoDB, Redis/IDistributedCache, IMemoryCache (L1), and Auth0. Existing tests covered domain entities only. Handler-level unit tests were missing.

## Decision

Write comprehensive unit tests for all 4 CRUD handler groups using xUnit + FluentAssertions + NSubstitute. Architecture tests extended to enforce VSA structural constraints.

## Test Files Created

| File | Tests | Status |
|------|-------|--------|
| `Handlers/GetBlogPostsHandlerTests.cs` | 4 | ✅ Pass |
| `Handlers/CreateBlogPostHandlerTests.cs` | 2 | ✅ Pass |
| `Handlers/DeleteBlogPostHandlerTests.cs` | 2 | ✅ Pass |
| `Handlers/EditBlogPostHandlerTests.cs` | 5 | ✅ Pass |
| `Architecture.Tests/VsaLayerTests.cs` | 3 | ✅ Pass |

**Total**: 20 unit tests + 6 architecture tests = 26 tests, 0 failures.

## Constraints Applied

- `IMemoryCache.Set<T>` is an extension method — mock `CreateEntry` instead.
- `IMemoryCache.TryGetValue` out-param: use `Returns(x => { x[1] = value; return true; })`.
- NetArchTest 1.3.2 has no `.Exist()` condition — use `GetTypes().Should().BeEmpty()`.
- No explicit `Microsoft.Extensions.Caching.*` package refs in Unit.Tests.csproj (causes NU1605); types come transitively via Web project reference.

## Consequences

- All handler business logic paths (cache hit L1, cache hit L2, cache miss, not found, exception) are tested.
- Architecture tests prevent re-introduction of InMemory repositories and enforce handler sealing and feature isolation.

---
### 2025-04-17: Project is MyBlog training blog app

**By:** Pippin (Docs)

**What:** MyBlog is a training/learning project to practice .NET Aspire orchestration, Blazor Server rendering, and clean architecture. Stack: .NET 10, Aspire 13.2.2, Blazor Server (Interactive Server Rendering), in-memory repository only. No database, no auth, no cache. Domain model: BlogPost entity with factory method and mutation methods. Clean architecture with Domain/Web layering. 9 tests total: 7 unit tests (BlogPost, InMemoryBlogPostRepository), 2 architecture tests (NetArchTest.Rules). All tests passing.

**Why:** User (Matthew Paulosky) directive — this is a hands-on learning project, not production. Captured for team memory to ensure all documentation and decisions stay scoped to actual project state.

**Implications:**
- Documentation always reflects training focus (no production concerns)
- Short project names (no MyBlog. prefix) — repo name provides context
- No external services (Auth0, MongoDB, Redis) — by design
- InMemoryBlogPostRepository is intentional (training)
- Tests are core to learning (TDD emphasis)

