// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AppHostTestCollection.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  AppHost.Tests
// =============================================

namespace AppHost.Tests.Infrastructure;

/// <summary>
/// Defines a single xUnit collection that shares one <see cref="AspireManager"/> instance across
/// all Playwright test classes. Running tests in a single collection ensures they execute
/// sequentially so the shared Aspire host, browser runtime, and test resources remain stable
/// across the AppHost browser suite.
/// </summary>
[CollectionDefinition(Name)]
public sealed class AppHostTestCollection : ICollectionFixture<AspireManager>
{
	/// <summary>Collection name used by <see cref="CollectionAttribute"/>.</summary>
	public const string Name = "AppHostTests";
}
