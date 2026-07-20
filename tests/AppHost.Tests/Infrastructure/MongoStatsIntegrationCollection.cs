// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     MongoStatsIntegrationCollection.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : MyBlog
// Project Name :  AppHost.Tests
// =============================================

namespace AppHost.Infrastructure;

/// <summary>
/// xUnit collection that shares one <see cref="ClearCommandAppFixture"/> across all tests
/// in the "MongoStatsIntegration" collection (sequential execution, single Aspire host).
/// </summary>
[CollectionDefinition("MongoStatsIntegration")]
public sealed class MongoStatsIntegrationCollection : ICollectionFixture<ClearCommandAppFixture>
{
}
