// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     SkipInCITheoryAttribute.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : MyBlog
// Project Name :  AppHost.Tests
// =============================================

namespace AppHost.Infrastructure;

/// <summary>
/// Custom [Theory] attribute that automatically skips theory-based tests when running in CI environment.
/// This is a convenient wrapper around [Theory] that applies CI environment detection.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
#pragma warning disable xUnit3003, xUnit3004
public sealed class SkipInCITheoryAttribute : TheoryAttribute
#pragma warning restore xUnit3003, xUnit3004
{
	private static bool IsCI => Environment.GetEnvironmentVariable("CI")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

	/// <summary>
	/// Initializes a new instance of the <see cref="SkipInCITheoryAttribute"/> class.
	/// Automatically skips if running in CI environment.
	/// </summary>
	public SkipInCITheoryAttribute()
	{
		if (IsCI)
		{
			Skip = "AppHost.Tests are skipped in CI environments due to hardcoded DCP 20-second timeout. " +
				"Aspire's Kubernetes initialization consistently exceeds this on cold-start (typical: 25-40 seconds). " +
				"Run tests locally for full E2E validation: 'dotnet test tests/AppHost.Tests'";
		}
	}
}
