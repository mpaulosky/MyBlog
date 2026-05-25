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
	public void AccountLogin_Should_ShortCircuitPlaceholderAuth0Config_BeforeOidcChallenge()
	{
		// Arrange
		var loginHandlerSource = ExtractLoginHandler(ReadRepoFile("src/Web/Program.cs"));
		var placeholderGuardIndex = loginHandlerSource.IndexOf("if (isPlaceholderAuth0Config)", StringComparison.Ordinal);
		var localRedirectIndex = loginHandlerSource.IndexOf(
			"ctx.Response.Redirect($\"/test/login?returnUrl={Uri.EscapeDataString(safeReturn)}\")",
			StringComparison.Ordinal);
		var challengeIndex = loginHandlerSource.IndexOf(
			"await ctx.ChallengeAsync(Auth0Constants.AuthenticationScheme, props).ConfigureAwait(false);",
			StringComparison.Ordinal);

		// Act / Assert
		placeholderGuardIndex.Should().BeGreaterThanOrEqualTo(
			0,
			because: "the login endpoint must detect placeholder Auth0 settings before trying OIDC");
		localRedirectIndex.Should().BeGreaterThan(
			placeholderGuardIndex,
			because: "placeholder Auth0 settings in Development/Testing should redirect to the local test login endpoint");
		challengeIndex.Should().BeGreaterThan(
			localRedirectIndex,
			because: "real Auth0 credentials must still use the normal OIDC challenge flow");
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
