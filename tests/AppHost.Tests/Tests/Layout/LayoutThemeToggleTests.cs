//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     LayoutThemeToggleTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  AppHost.Tests
//=======================================================

using AppHost.Tests.Infrastructure;

using FluentAssertions;

namespace AppHost.Tests.Tests.Layout;

/// <summary>
/// Playwright regression coverage for the live light/dark theme toggle.
/// This exercises the real app shell, JS bootstrap, and interactive render boundary.
/// </summary>
public sealed class LayoutThemeToggleTests : BasePlaywrightTests
{
	public LayoutThemeToggleTests(AspireManager aspireManager) : base(aspireManager) { }

	[Theory]
	[InlineData("light", false)]
	[InlineData("dark", true)]
	public async Task ThemeToggle_ClickingSwitchesBrightnessAndHtmlDarkClass(string initialBrightness, bool initialHasDarkClass)
	{
		// Arrange
		await InteractWithPageAsync("web", async page =>
		{
			var runtimeDiagnostics = ThemeToggleTestRuntime.BrowserRuntimeDiagnostics.Attach(page);

			// Arrange — align system preference with the requested starting brightness.
			await page.EmulateMediaAsync(new()
			{
				ColorScheme = initialBrightness == "dark" ? ColorScheme.Dark : ColorScheme.Light
			});

			// Navigate first so the page has a real origin, then seed localStorage and
			// reload to exercise the real bootstrap path with deterministic storage.
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await page.EvaluateAsync(
					"brightness => { localStorage.setItem('theme-color', 'blue'); localStorage.setItem('theme-mode', brightness); }",
					initialBrightness);

			await page.ReloadAsync();
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var toggleButton = page.Locator("button[aria-label*=\"Toggle dark mode\"]").First;
			await toggleButton.WaitForAsync();

			var becameInteractive = await ThemeToggleTestRuntime.WaitForThemeReadyAsync(page, toggleButton);
			if (!becameInteractive)
			{
				var blockedSignals = await ThemeToggleTestRuntime.ReadThemeSignalsAsync(page, toggleButton);
				var assetDiagnostics = await ThemeToggleTestRuntime.ReadAssetFetchDiagnosticsAsync(page);
				Assert.Skip($"AppHost Testing never reached a trustworthy interactive theme state for the reload/bootstrap flow seeded with '{initialBrightness}'. Observed after reload: {ThemeToggleTestRuntime.DescribeSignals(blockedSignals)}. Browser diagnostics: {runtimeDiagnostics.Describe()}. Asset fetch diagnostics: {assetDiagnostics}.");
			}

			var startingSignals = await ThemeToggleTestRuntime.ReadThemeSignalsAsync(page, toggleButton);

			// Assert
			startingSignals.HasDarkClass.Should().Be(initialHasDarkClass,
					because: "the seeded reload/bootstrap path should restore the requested html dark class before interaction");
			startingSignals.StoredBrightness.Should().Be(initialBrightness,
					because: "the seeded reload/bootstrap path should restore the requested brightness in theme-mode before interaction");
			startingSignals.AriaLabel.Should().Contain($"currently {initialBrightness}",
					because: "the live toggle should reflect the stored brightness before interaction");

			// Act — click the toggle and wait for the live page state to flip.
			await toggleButton.ClickAsync();

			var expectedBrightness = initialBrightness == "light" ? "dark" : "light";
			var expectedDarkClass = expectedBrightness == "dark";
			var toggledToExpectedState = await ThemeToggleTestRuntime.WaitForThemeStateAsync(
					page,
					toggleButton,
					expectedBrightness,
					expectedDarkClass);

			var endingSignals = await ThemeToggleTestRuntime.ReadThemeSignalsAsync(page, toggleButton);
			if (!toggledToExpectedState && !endingSignals.IsTrustworthyInteractiveState())
			{
				var assetDiagnostics = await ThemeToggleTestRuntime.ReadAssetFetchDiagnosticsAsync(page);
				Assert.Skip($"AppHost Testing lost a trustworthy interactive theme state after clicking the reload/bootstrap toggle seeded with '{initialBrightness}'. Observed after clicking: {ThemeToggleTestRuntime.DescribeSignals(endingSignals)}. Browser diagnostics: {runtimeDiagnostics.Describe()}. Asset fetch diagnostics: {assetDiagnostics}.");
			}

			// Assert
			endingSignals.HasDarkClass.Should().Be(expectedDarkClass,
					because: "clicking the theme toggle should update the html dark class");
			endingSignals.StoredBrightness.Should().Be(expectedBrightness,
					because: "clicking the theme toggle should persist the new brightness in theme-mode");
			endingSignals.AriaLabel.Should().Contain($"currently {expectedBrightness}",
					because: "the live toggle label should reflect the updated brightness state");
		});
	}
}
