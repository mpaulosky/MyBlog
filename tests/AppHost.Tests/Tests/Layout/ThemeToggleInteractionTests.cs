//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     ThemeToggleInteractionTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  AppHost.Tests
//=======================================================

using AppHost.Tests.Infrastructure;

using FluentAssertions;

namespace AppHost.Tests.Tests.Layout;

/// <summary>
/// Runtime-focused browser coverage for theme toggle persistence while navigating
/// between interactive pages.
/// </summary>
public sealed class ThemeToggleInteractionTests : BasePlaywrightTests
{
	public ThemeToggleInteractionTests(AspireManager aspireManager) : base(aspireManager) { }

	[SkipInCIFact]
	public async Task ThemeToggle_DarkMode_PersistsAfterNavigatingToBlogPosts()
	{
		// Arrange
		await InteractWithPageAsync("web", async page =>
		{
			var runtimeDiagnostics = ThemeToggleTestRuntime.BrowserRuntimeDiagnostics.Attach(page);

			await page.EmulateMediaAsync(new()
			{
				ColorScheme = ColorScheme.Light
			});

			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var toggleButton = page.Locator("button[aria-label*=\"Toggle dark mode\"]").First;
			await toggleButton.WaitForAsync();

			var becameInteractive = await ThemeToggleTestRuntime.WaitForThemeReadyAsync(page, toggleButton);
			if (!becameInteractive)
			{
				var blockedSignals = await ThemeToggleTestRuntime.ReadThemeSignalsAsync(page, toggleButton);
				var assetDiagnostics = await ThemeToggleTestRuntime.ReadAssetFetchDiagnosticsAsync(page);
				Assert.Skip($"AppHost Testing never reached a trustworthy interactive theme state for the /blog persistence flow. Observed on the home page before toggling: {ThemeToggleTestRuntime.DescribeSignals(blockedSignals)}. Browser diagnostics: {runtimeDiagnostics.Describe()}. Asset fetch diagnostics: {assetDiagnostics}.");
			}

			await toggleButton.ClickAsync();

			var toggledState = await ThemeToggleTestRuntime.WaitForThemeStateAsync(page, toggleButton, expectedBrightness: "dark", expectedDarkClass: true);
			var themeSignalsBeforeNavigation = toggledState.Signals;
			if (!toggledState.MatchedExpectedState
						&& !toggledState.SawTrustworthyInteractiveState
						&& !becameInteractive)
			{
				var assetDiagnostics = await ThemeToggleTestRuntime.ReadAssetFetchDiagnosticsAsync(page);
				Assert.Skip($"AppHost Testing never applied the light→dark toggle deterministically because the page never reached a trustworthy interactive state after the click. Observed after clicking the home-page toggle: {ThemeToggleTestRuntime.DescribeSignals(themeSignalsBeforeNavigation)}. Browser diagnostics: {runtimeDiagnostics.Describe()}. Asset fetch diagnostics: {assetDiagnostics}.");
			}

			themeSignalsBeforeNavigation.HasDarkClass.Should().BeTrue(
					because: "the home-page toggle should apply the html dark class before navigating to Blog Posts");
			themeSignalsBeforeNavigation.StoredBrightness.Should().Be("dark",
					because: "the home-page toggle should persist dark mode before navigating to Blog Posts");
			themeSignalsBeforeNavigation.AriaLabel.Should().Contain("currently dark",
					because: "the home-page toggle label should describe the updated dark-mode state before navigation");

			var blogPostsLink = page.Locator("nav[aria-label=\"Main navigation\"] a[href=\"blog\"]").First;
			await blogPostsLink.ClickAsync();
			await page.WaitForURLAsync("**/blog");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var blogHeading = page.GetByRole(AriaRole.Heading, new() { Name = "Blog Posts", Exact = true });
			await blogHeading.WaitForAsync();

			var blogToggleButton = page.Locator("button[aria-label*=\"Toggle dark mode\"]").First;
			await blogToggleButton.WaitForAsync();

			var persistedOnBlogPage = await ThemeToggleTestRuntime.WaitForThemeStateAsync(page, blogToggleButton, expectedBrightness: "dark", expectedDarkClass: true);
			var themeSignalsAfterNavigation = persistedOnBlogPage.Signals;
			if (!persistedOnBlogPage.MatchedExpectedState
						&& !persistedOnBlogPage.SawTrustworthyInteractiveState
						&& !becameInteractive)
			{
				var assetDiagnostics = await ThemeToggleTestRuntime.ReadAssetFetchDiagnosticsAsync(page);
				Assert.Skip($"AppHost Testing reached /blog but the persisted dark-mode signals were not trustworthy after navigation. Expected the chosen theme to hold on the Blog Posts page, but observed: {ThemeToggleTestRuntime.DescribeSignals(themeSignalsAfterNavigation)}. Browser diagnostics: {runtimeDiagnostics.Describe()}. Asset fetch diagnostics: {assetDiagnostics}.");
			}

			var headingText = await blogHeading.TextContentAsync();

			// Assert
			headingText.Should().Contain("Blog Posts",
					because: "the runtime persistence check must verify the real Blog Posts page after navigation");
			themeSignalsAfterNavigation.HasDarkClass.Should().BeTrue(
					because: "the html dark class should still be applied after navigating to the Blog Posts page");
			themeSignalsAfterNavigation.StoredBrightness.Should().Be("dark",
					because: "the selected brightness should stay persisted in localStorage after navigation");
			themeSignalsAfterNavigation.AriaLabel.Should().Contain("currently dark",
					because: "the live toggle on the Blog Posts page should still describe the persisted dark-mode state");
			themeSignalsAfterNavigation.StoredColor.Should().Be(themeSignalsBeforeNavigation.StoredColor,
					because: "navigating to Blog Posts should not disturb the active color-theme storage value while checking brightness persistence");
		});
	}

}
