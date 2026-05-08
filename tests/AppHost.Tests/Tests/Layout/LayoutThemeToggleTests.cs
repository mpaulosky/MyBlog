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

	[Theory(Skip = "The reload/bootstrap persistence path still flakes in AppHost because a seeded localStorage reload can race the post-hydration readiness marker; click-through runtime coverage lives in ThemeToggleInteractionTests.")]
	[InlineData("light", false)]
	[InlineData("dark", true)]
	public async Task ThemeToggle_ClickingSwitchesBrightnessAndHtmlDarkClass(string initialBrightness, bool initialHasDarkClass)
	{
		// Arrange / Act / Assert
		await InteractWithPageAsync("web", async page =>
		{
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
			await page.WaitForTimeoutAsync(5000);

			var toggleButton = page.Locator("button[aria-label*=\"Toggle dark mode\"]").First;
			await toggleButton.WaitForAsync();

			var initialDeadline = DateTime.UtcNow.AddSeconds(10);
			var startingDarkClass = false;
			string? startingStoredBrightness = null;
			string? initialLabel = null;

			while (DateTime.UtcNow < initialDeadline)
			{
				startingDarkClass = await page.EvaluateAsync<bool>("() => document.documentElement.classList.contains('dark')");
				startingStoredBrightness = await page.EvaluateAsync<string?>("() => localStorage.getItem('theme-mode')");
				initialLabel = await toggleButton.GetAttributeAsync("aria-label");

				if (startingDarkClass == initialHasDarkClass
					&& startingStoredBrightness == initialBrightness
					&& (initialLabel?.Contains($"currently {initialBrightness}", StringComparison.Ordinal) ?? false))
				{
					break;
				}

				await Task.Delay(250);
			}

			// Assert — precondition matches the stored theme mode.
			startingDarkClass.Should().Be(initialHasDarkClass);
			startingStoredBrightness.Should().Be(initialBrightness);
			initialLabel.Should().Contain($"currently {initialBrightness}",
					because: "the live toggle should reflect the stored brightness before interaction");

			// Act — click the toggle and wait for the live page state to flip.
			await toggleButton.ClickAsync();

			var expectedBrightness = initialBrightness == "light" ? "dark" : "light";
			var expectedDarkClass = expectedBrightness == "dark";

			var endingDeadline = DateTime.UtcNow.AddSeconds(10);
			var endingDarkClass = startingDarkClass;
			var endingStoredBrightness = startingStoredBrightness;
			var ariaLabel = initialLabel;

			while (DateTime.UtcNow < endingDeadline)
			{
				endingDarkClass = await page.EvaluateAsync<bool>("() => document.documentElement.classList.contains('dark')");
				endingStoredBrightness = await page.EvaluateAsync<string?>("() => localStorage.getItem('theme-mode')");
				ariaLabel = await toggleButton.GetAttributeAsync("aria-label");

				if (endingDarkClass == expectedDarkClass
					&& endingStoredBrightness == expectedBrightness
					&& (ariaLabel?.Contains($"currently {expectedBrightness}", StringComparison.Ordinal) ?? false))
				{
					break;
				}

				await Task.Delay(250);
			}

			endingDarkClass.Should().Be(expectedDarkClass,
					because: "clicking the theme toggle should update the html dark class");
			endingStoredBrightness.Should().Be(expectedBrightness,
					because: "clicking the theme toggle should persist the new brightness in theme-mode");
			ariaLabel.Should().Contain($"currently {expectedBrightness}",
					because: "the live toggle label should reflect the updated brightness state");
		});
	}
}
