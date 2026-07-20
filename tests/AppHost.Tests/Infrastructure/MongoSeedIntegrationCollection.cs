// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     MongoSeedIntegrationCollection.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : MyBlog
// Project Name :  AppHost.Tests
// =============================================

namespace AppHost.Infrastructure;

/// <summary>
/// xUnit collection that shares one <see cref="ClearCommandAppFixture"/> across all tests
/// in the "MongoSeedIntegration" collection (sequential execution, single Aspire host).
/// </summary>
[CollectionDefinition("MongoSeedIntegration")]
public sealed class MongoSeedIntegrationCollection : ICollectionFixture<ClearCommandAppFixture>
{
}
