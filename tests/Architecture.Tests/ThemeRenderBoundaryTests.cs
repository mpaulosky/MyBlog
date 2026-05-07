//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     ThemeRenderBoundaryTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Architecture.Tests
//=======================================================

namespace MyBlog.Architecture.Tests;

public sealed class ThemeRenderBoundaryTests
{
	private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

	[Fact]
	public void RoutesShouldWrapRouterInsideThemeProvider()
	{
		// Arrange
		var routesMarkup = ReadRepoFile("src/Web/Components/Routes.razor");
		var themeProviderStart = routesMarkup.IndexOf("<ThemeProvider>", StringComparison.Ordinal);
		var routerStart = routesMarkup.IndexOf("<Router", StringComparison.Ordinal);
		var routerEnd = routesMarkup.IndexOf("</Router>", StringComparison.Ordinal);
		var themeProviderEnd = routesMarkup.IndexOf("</ThemeProvider>", StringComparison.Ordinal);

		// Act / Assert — guard every index before comparing order so a missing tag cannot false-pass
		themeProviderStart.Should().BeGreaterThanOrEqualTo(0,
				because: "ThemeProvider must exist in Routes.razor so it shares the interactive subtree with ThemeSelector");
		routerStart.Should().BeGreaterThanOrEqualTo(0,
				because: "<Router must be present in Routes.razor");
		routerEnd.Should().BeGreaterThanOrEqualTo(0,
				because: "</Router> closing tag must be present in Routes.razor before order can be verified");
		themeProviderEnd.Should().BeGreaterThanOrEqualTo(0,
				because: "</ThemeProvider> closing tag must be present in Routes.razor before order can be verified");
		routerStart.Should().BeGreaterThan(themeProviderStart,
				because: "ThemeProvider should wrap the router, not sit outside the interactive routes boundary");
		themeProviderEnd.Should().BeGreaterThan(routerEnd,
				because: "ThemeProvider should close after the router so layout consumers stay inside the cascade");
	}

	[Fact]
	public void AppShouldRenderInteractiveRoutesWithoutWrappingThemeProvider()
	{
		// Arrange
		var appMarkup = ReadRepoFile("src/Web/Components/App.razor");

		// Act / Assert
		appMarkup.Should().Contain("<Routes @rendermode=\"InteractiveServer\" />",
				because: "the interactive routes boundary must remain in App.razor");
		appMarkup.Should().NotContain("<ThemeProvider>",
				because: "ThemeProvider belongs in Routes.razor, not outside the interactive routes boundary in App.razor");
	}

	[Fact]
	public void NavMenuShouldNotDeclareItsOwnRenderMode()
	{
		// Arrange
		var navMenuMarkup = ReadRepoFile("src/Web/Components/Layout/NavMenu.razor");

		// Act / Assert
		navMenuMarkup.Should().NotContain("@rendermode",
				because: "a nested render boundary can isolate ThemeSelector from the ThemeProvider cascade");
	}

	[Fact]
	public void NavMenuShouldContainThemeSelectorElement()
	{
		// Arrange
		var navMenuMarkup = ReadRepoFile("src/Web/Components/Layout/NavMenu.razor");

		// Act / Assert
		navMenuMarkup.Should().Contain("<ThemeSelector />",
				because: "NavMenu must include ThemeSelector so the toggle and color picker are reachable from every page");
	}

	private static string ReadRepoFile(string relativePath)
	{
		return File.ReadAllText(Path.Combine(RepoRoot, relativePath));
	}
}
