// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     MongoClearIntegrationCollection.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : MyBlog
// Project Name :  AppHost.Tests
// =============================================

namespace AppHost.Infrastructure;

/// <summary>
/// xUnit collection that shares one <see cref="ClearCommandAppFixture"/> across all tests
/// in the "MongoClearIntegration" collection (sequential execution, single Aspire host).
/// </summary>
[CollectionDefinition("MongoClearIntegration")]
public sealed class MongoClearIntegrationCollection : ICollectionFixture<ClearCommandAppFixture>
{
}
