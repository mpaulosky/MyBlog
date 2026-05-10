//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     ProfileEmailAuthContractTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Architecture.Tests
//=======================================================

using System.Text.RegularExpressions;

namespace MyBlog.Architecture.Tests;

public sealed class ProfileEmailAuthContractTests
{
	private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

	[Fact]
	public void ProgramShouldRequestEmailScopeForAuth0WebLogin()
	{
		// Arrange
		var programSource = ReadRepoFile("src/Web/Program.cs");
		var configuresEmailScope =
			Regex.IsMatch(
				programSource,
				@"\.Scope\s*=\s*""[^""]*\bemail\b[^""]*""",
				RegexOptions.CultureInvariant)
			|| Regex.IsMatch(
				programSource,
				@"\.WithScope\s*\(\s*""[^""]*\bemail\b[^""]*""\s*\)",
				RegexOptions.CultureInvariant);

		// Act / Assert
		configuresEmailScope.Should().BeTrue(
				because: "Auth0's ASP.NET Core SDK defaults to 'openid profile', so the web app must explicitly request the email scope if Profile.razor expects the signed-in principal to carry an email claim");
	}

	private static string ReadRepoFile(string relativePath)
	{
		return File.ReadAllText(Path.Combine(RepoRoot, relativePath));
	}
}
