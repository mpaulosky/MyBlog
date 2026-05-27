//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     AuthFallbackContractTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Architecture.Tests
//=======================================================

namespace MyBlog.Architecture.Tests;

public sealed class AuthFallbackContractTests
{
	private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

	[Fact]
	public void AccountLogin_Should_ShortCircuitPlaceholderAuth0Config_OnlyInTesting_BeforeOidcChallenge()
	{
		// Arrange
		var loginHandlerSource = ExtractLoginHandler(ReadRepoFile("src/Web/Program.cs"));
		var placeholderGuardIndex = loginHandlerSource.IndexOf(
			"if (usesLocalTestLoginFallback && app.Environment.IsEnvironment(\"Testing\"))",
			StringComparison.Ordinal);
		var localRedirectIndex = loginHandlerSource.IndexOf(
			"ctx.Response.Redirect($\"/test/login?returnUrl={Uri.EscapeDataString(safeReturn)}\")",
			StringComparison.Ordinal);
		var challengeIndex = loginHandlerSource.IndexOf(
			"await ctx.ChallengeAsync(Auth0Constants.AuthenticationScheme, props).ConfigureAwait(false);",
			StringComparison.Ordinal);

		// Act / Assert
		placeholderGuardIndex.Should().BeGreaterThanOrEqualTo(
			0,
			because: "only the Testing environment may bypass Auth0 and short-circuit to the local test login");
		localRedirectIndex.Should().BeGreaterThan(
			placeholderGuardIndex,
			because: "the Testing fallback must redirect to the local test login endpoint before issuing any OIDC challenge");
		challengeIndex.Should().BeGreaterThan(
			localRedirectIndex,
			because: "real Auth0 credentials must still use the normal OIDC challenge flow");
	}

	[Fact]
	public void TestLoginEndpoint_Should_Be_Registered_OnlyInTesting()
	{
		// Arrange
		var programSource = ReadRepoFile("src/Web/Program.cs");
		var testingGuardIndex = programSource.IndexOf(
			"if (app.Environment.IsEnvironment(\"Testing\"))",
			StringComparison.Ordinal);
		var testLoginEndpointIndex = programSource.IndexOf(
			"app.MapGet(\"/test/login\", MapTestLoginEndpoint).AllowAnonymous();",
			StringComparison.Ordinal);
		var legacyDevelopmentGuardIndex = programSource.IndexOf(
			"if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment(\"Testing\"))",
			StringComparison.Ordinal);

		// Act / Assert
		testingGuardIndex.Should().BeGreaterThanOrEqualTo(
			0,
			because: "the local test login endpoint must be fenced to the Testing environment");
		testLoginEndpointIndex.Should().BeGreaterThan(
			testingGuardIndex,
			because: "the Testing-only guard must wrap the local test login endpoint registration");
		legacyDevelopmentGuardIndex.Should().BeLessThan(
			0,
			because: "Development should go through real Auth0 rather than the local test login endpoint");
	}

	private static string ExtractLoginHandler(string programSource)
	{
		var start = programSource.IndexOf("app.MapGet(\"/Account/Login\"", StringComparison.Ordinal);
		start.Should().BeGreaterThanOrEqualTo(0, because: "Program.cs should map the login endpoint");

		var end = programSource.IndexOf("}).AllowAnonymous();", start, StringComparison.Ordinal);
		end.Should().BeGreaterThan(start, because: "the login endpoint should remain explicitly anonymous");

		return programSource[start..end];
	}

	private static string ReadRepoFile(string relativePath)
	{
		return File.ReadAllText(Path.Combine(RepoRoot, relativePath));
	}
}
