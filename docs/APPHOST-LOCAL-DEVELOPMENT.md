# AppHost Local Development Guide

This document covers AppHost orchestration for local development, including MongoDB seeding, the clear-and-reseed workflow, and managing canonical category seed data.

## Overview

AppHost is the .NET Aspire orchestration layer that bootstraps MongoDB, Redis, and the Web application. It provides three developer commands for managing local database state:

1. **🌱 Seed MyBlog Data** — Upserts the canonical seven categories and inserts seed blog posts
2. **⚠️ Clear MyBlog Data** — Deletes all data from the local MongoDB database
3. **📊 Show MyBlog Stats** — Displays collection and document counts

## Running AppHost

### Start the Aspire Dashboard

```bash
cd src/AppHost
dotnet run
```

This launches the Aspire dashboard at `http://localhost:15100` (default port). The dashboard displays:

- Running services (Web, MongoDB, Redis) with health status
- Service logs and metrics
- Resource usage and performance data
- Command execution interface for database operations

### Accessing Database Commands

The MongoDB resource card in the Aspire dashboard shows three commands in the "Commands" section:

| Command | Effect | Use When |
|---------|--------|----------|
| **🌱 Seed MyBlog Data** | Upserts 7 canonical categories + inserts 3 seed blog posts | First setup or after clearing data |
| **⚠️ Clear MyBlog Data** | Deletes all documents from all non-system collections | Resetting to a clean state |
| **📊 Show MyBlog Stats** | Displays collection names and document counts | Verifying seeded state |

## Canonical Category Dataset

The seven canonical categories are the source of truth for blog post categorization:

| # | Name | ObjectId | Use Case |
|---|------|----------|----------|
| 1 | ASP.NET Core | `677db927900ea4af1b500cab` | ASP.NET and web framework posts |
| 2 | Blazor Server | `677db927900ea4af1b500cac` | Blazor Server rendering posts |
| 3 | Blazor WebAssembly | `677db9bd900ea4af1b500cad` | Blazor WebAssembly posts |
| 4 | C# | `677db9bd900ea4af1b500cae` | C# language feature posts |
| 5 | Entity Framework Core (EF Core) | `677db9bd900ea4af1b500caf` | EF Core and data access posts |
| 6 | .NET MAUI | `677db9bd900ea4af1b500cb0` | Mobile development posts |
| 7 | Other | `677db9bd900ea4af1b500cb1` | Miscellaneous or unclassified posts |

**Important**: These ObjectIds are hardcoded in the AppHost seeding logic and referenced by all seeded blog posts. They must never change; the test suite verifies that seeded posts reference only these seven categories by their exact ObjectIds.

**Reference**: The canonical categories are also documented in `docs/Category-Seed-Data` (read-only reference).

## Clear-and-Reseed Workflow for Local Development

When canonical category seed data changes, or when you want to reset your local database to a known state, use this workflow:

### Step 1: Clear All Data

1. Open the Aspire dashboard (`http://localhost:15100`)
2. Locate the **MongoDB** resource card
3. Click the **⚠️ Clear MyBlog Data** command
4. Confirm the destructive operation when prompted
5. Wait for the operation to complete — the command output will show:

   ```text
   {n} collection(s) cleared — {m} total document(s) deleted.
   ```

### Step 2: Verify Clean State

1. In the same MongoDB resource card, click **📊 Show MyBlog Stats**
2. Confirm the output shows no collections, or only system collections:

   ```text
   *(no collections found)* | -
   ```

### Step 3: Reseed with Current Data

1. Click **🌱 Seed MyBlog Data** on the MongoDB resource
2. Wait for completion — the output will show:

   ```text
   categories: 7 upserted (...); blogposts: {n} inserted (... published, ... draft)
   ```

3. Verify with **📊 Show MyBlog Stats** — you should see:
   - **categories**: 7 documents
   - **blogposts**: 3 documents (as of this version)

### Manual Clear-and-Reseed (Alternative)

If the Aspire dashboard commands are unavailable, you can clear and reseed using `mongosh`:

```bash
# Connect to local MongoDB
mongosh "mongodb://localhost:27017"

# In the mongosh shell:
use myblog
db.categories.deleteMany({})
db.blogposts.deleteMany({})

# Exit and reseed via the AppHost dashboard command
exit
```

## Seeding Logic and Idempotency

### Upsert Behavior for Categories

Categories are upserted (insert if missing, replace if exists) using their ObjectId as the match key:

```csharp
await categoriesCollection.ReplaceOneAsync(
    Builders<BsonDocument>.Filter.Eq("_id", category["_id"]),
    category,
    new ReplaceOptions { IsUpsert = true }
);
```

This ensures:

- Running seed multiple times is safe (existing categories are replaced with the current definition)
- If a category is deleted manually, re-running seed will recreate it
- The seeding operation is idempotent

### Insert Behavior for Blog Posts

Seed blog posts are inserted (not upserted). If you run seed twice without clearing, you will get duplicate blog post documents. Always clear before reseeding to maintain a clean state.

## When to Clear and Reseed

**Clear and reseed your local database if:**

- You want to start fresh with the current seed data
- The seed data definition in AppHost has changed (e.g., new categories, updated blog posts)
- Your local database is corrupted or in an inconsistent state
- You are testing the seeding behavior or category-to-post references
- A team member has updated `docs/Category-Seed-Data` or the seeding logic in `src/AppHost/MongoDbResourceBuilderExtensions.cs`

**Do NOT** clear and reseed if:

- You are actively testing with custom data
- Your local database contains work-in-progress blog posts or categories
- Integration tests are running (they manage their own Testcontainer instances)

## Seed Data Details

### Categories

All seven categories are defined in `src/AppHost/MongoDbResourceBuilderExtensions.cs` (lines 240–249):

```csharp
var canonicalCategories = new BsonDocument[]
{
    new() { ["_id"] = new ObjectId("677db927900ea4af1b500cab"), ["Name"] = "ASP.NET Core", ... },
    new() { ["_id"] = new ObjectId("677db927900ea4af1b500cac"), ["Name"] = "Blazor Server", ... },
    // ... (5 more categories)
};
```

### Blog Posts

Seed blog posts (currently 3: 2 published + 1 draft) are inserted with CategoryId references to the canonical ObjectIds:

- **"Welcome to MyBlog"** → `Other` category (`677db9bd900ea4af1b500cb1`)
- **"Getting Started with .NET Aspire"** → `ASP.NET Core` category (`677db927900ea4af1b500cab`)
- **"Draft: MongoDB Performance Tips"** → `Other` category (`677db9bd900ea4af1b500cb1`)

All posts are authored by "Matthew Paulosky" with the Auth0 ID `auth0|author-matthew-paulosky`.

## Troubleshooting

### Command Not Responding

**Symptom**: The seed, clear, or stats command hangs or times out.

**Cause**: Another database operation is running. AppHost uses a shared semaphore (`_dbMutex`) to ensure only one command executes at a time.

**Solution**: Wait for the current operation to complete, then retry. You can check Aspire logs to see which command is in progress.

### Seed Data Missing After Reseeding

**Symptom**: You ran seed, but blog posts or categories are not appearing.

**Cause**: The seed operation may have failed silently, or you did not wait for completion.

**Solution**:

1. Check the command output in the Aspire dashboard for error messages
2. Verify with **📊 Show MyBlog Stats** that collections exist
3. Clear and reseed again if needed

### Categories Mismatch Between Local and Tests

**Symptom**: Tests pass but seeded blog posts reference categories that don't exist in your local database.

**Cause**: Your local seed data is stale or differs from the current AppHost seeding logic.

**Solution**:

1. Pull the latest code from `dev`
2. Clear your local database with **⚠️ Clear MyBlog Data**
3. Reseed with **🌱 Seed MyBlog Data**
4. Verify with **📊 Show MyBlog Stats**

## Related Documentation

- [ARCHITECTURE.md](ARCHITECTURE.md) — Overall AppHost orchestration and service composition
- [docs/Category-Seed-Data](Category-Seed-Data) — Reference-only canonical category ObjectIds
- [CONTRIBUTING.md](CONTRIBUTING.md) — Build, test, and development workflow

## Questions?

See [CONTRIBUTING.md](CONTRIBUTING.md) for contact information.
