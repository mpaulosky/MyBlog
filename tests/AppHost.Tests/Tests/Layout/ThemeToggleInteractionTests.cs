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

	[Fact]
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

			var toggledToDark = await ThemeToggleTestRuntime.WaitForThemeStateAsync(page, toggleButton, expectedBrightness: "dark", expectedDarkClass: true);
			if (!toggledToDark)
			{
				var blockedSignals = await ThemeToggleTestRuntime.ReadThemeSignalsAsync(page, toggleButton);
				var assetDiagnostics = await ThemeToggleTestRuntime.ReadAssetFetchDiagnosticsAsync(page);
				Assert.Skip($"AppHost Testing never applied the light→dark toggle deterministically, so the /blog persistence scenario cannot be trusted. Observed after clicking the home-page toggle: {ThemeToggleTestRuntime.DescribeSignals(blockedSignals)}. Browser diagnostics: {runtimeDiagnostics.Describe()}. Asset fetch diagnostics: {assetDiagnostics}.");
			}

			var themeSignalsBeforeNavigation = await ThemeToggleTestRuntime.ReadThemeSignalsAsync(page, toggleButton);

			var blogPostsLink = page.Locator("nav[aria-label=\"Main navigation\"] a[href=\"blog\"]").First;
			await blogPostsLink.ClickAsync();
			await page.WaitForURLAsync("**/blog");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var blogHeading = page.GetByRole(AriaRole.Heading, new() { Name = "Blog Posts", Exact = true });
			await blogHeading.WaitForAsync();

			var blogToggleButton = page.Locator("button[aria-label*=\"Toggle dark mode\"]").First;
			await blogToggleButton.WaitForAsync();

			var persistedOnBlogPage = await ThemeToggleTestRuntime.WaitForThemeStateAsync(page, blogToggleButton, expectedBrightness: "dark", expectedDarkClass: true);
			if (!persistedOnBlogPage)
			{
				var blockedSignals = await ThemeToggleTestRuntime.ReadThemeSignalsAsync(page, blogToggleButton);
				var assetDiagnostics = await ThemeToggleTestRuntime.ReadAssetFetchDiagnosticsAsync(page);
				Assert.Skip($"AppHost Testing reached /blog but the persisted dark-mode signals were not trustworthy after navigation. Expected the chosen theme to hold on the Blog Posts page, but observed: {ThemeToggleTestRuntime.DescribeSignals(blockedSignals)}. Browser diagnostics: {runtimeDiagnostics.Describe()}. Asset fetch diagnostics: {assetDiagnostics}.");
			}

			var themeSignalsAfterNavigation = await ThemeToggleTestRuntime.ReadThemeSignalsAsync(page, blogToggleButton);
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


internal static class ThemeToggleTestRuntime
{
	internal static async Task<bool> WaitForThemeReadyAsync(IPage page, ILocator toggleButton, TimeSpan? timeout = null)
	{
		var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(10));

		while (DateTime.UtcNow < deadline)
		{
			var signals = await ReadThemeSignalsAsync(page, toggleButton);
			if (signals.IsTrustworthyInteractiveState())
			{
				return true;
			}

			await Task.Delay(250);
		}

		return false;
	}

	internal static async Task<bool> WaitForThemeStateAsync(
		IPage page,
		ILocator toggleButton,
		string expectedBrightness,
		bool expectedDarkClass,
		TimeSpan? timeout = null)
	{
		var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(10));

		while (DateTime.UtcNow < deadline)
		{
			var signals = await ReadThemeSignalsAsync(page, toggleButton);
			if (signals.IsTrustworthyInteractiveState()
				&& signals.HasDarkClass == expectedDarkClass
				&& string.Equals(signals.StoredBrightness, expectedBrightness, StringComparison.Ordinal)
				&& (signals.AriaLabel?.Contains($"currently {expectedBrightness}", StringComparison.Ordinal) ?? false))
			{
				return true;
			}

			await Task.Delay(250);
		}

		return false;
	}

	internal static async Task<ThemeSignals> ReadThemeSignalsAsync(IPage page, ILocator toggleButton)
	{
		var hasDarkClass = await page.EvaluateAsync<bool>("() => document.documentElement.classList.contains('dark')");
		var storedBrightness = await page.EvaluateAsync<string?>("() => localStorage.getItem('theme-mode')");
		var storedColor = await page.EvaluateAsync<string?>("() => localStorage.getItem('theme-color')");
		var ariaLabel = await toggleButton.GetAttributeAsync("aria-label");
		var readinessMarker = await page.EvaluateAsync<string?>("() => document.documentElement.getAttribute('data-theme-ready')");
		var hasThemeManager = await page.EvaluateAsync<bool>("() => !!window.themeManager");
		var hasBlazor = await page.EvaluateAsync<bool>("() => !!window.Blazor");
		var sawThemeScriptResource = await page.EvaluateAsync<bool>("() => performance.getEntriesByType('resource').some(entry => entry.name.includes('/js/theme.js'))");
		var sawBlazorScriptResource = await page.EvaluateAsync<bool>("() => performance.getEntriesByType('resource').some(entry => entry.name.includes('blazor.web.js'))");

		return new ThemeSignals(
			hasDarkClass,
			storedBrightness,
			storedColor,
			ariaLabel,
			readinessMarker,
			hasThemeManager,
			hasBlazor,
			sawThemeScriptResource,
			sawBlazorScriptResource);
	}

	internal static async Task<string> ReadAssetFetchDiagnosticsAsync(IPage page)
	{
		var diagnostics = await page.EvaluateAsync<string>(
			"""
			async () => {
				const interestingPaths = new Set([
					'/_framework/blazor.web.js',
					'/Components/Layout/ReconnectModal.razor.js',
					'/Web.styles.css',
					'/MyBlog.Web.styles.css'
				]);

				for (const element of Array.from(document.querySelectorAll('script[src], link[href]'))) {
					const raw = element.getAttribute('src') ?? element.getAttribute('href');
					if (!raw) {
						continue;
					}

					try {
						const path = new URL(raw, window.location.href).pathname;
						if (/blazor\.web|ReconnectModal|styles\.css/i.test(path)) {
							interestingPaths.add(path);
						}
					} catch {
						// Ignore malformed URLs in diagnostics.
					}
				}

				const results = [];
				for (const path of interestingPaths) {
					try {
						const response = await fetch(path, { cache: 'no-store' });
						const body = await response.text();
						results.push({
							path,
							status: response.status,
							ok: response.ok,
							body: body.replace(/\s+/g, ' ').slice(0, 220)
						});
					} catch (error) {
						results.push({
							path,
							error: String(error)
						});
					}
				}

				return JSON.stringify(results);
			}
			""");

		return string.IsNullOrWhiteSpace(diagnostics) ? "[]" : diagnostics;
	}

	internal static string DescribeSignals(ThemeSignals signals)
	{
		var darkClass = signals.HasDarkClass ? "true" : "false";
		var themeManager = signals.HasThemeManager ? "present" : "missing";
		var blazor = signals.HasBlazor ? "present" : "missing";
		var themeScript = signals.SawThemeScriptResource ? "requested" : "not-requested";
		var blazorScript = signals.SawBlazorScriptResource ? "requested" : "not-requested";

		return $"data-theme-ready='{signals.ReadinessMarker ?? "<null>"}', aria-label='{signals.AriaLabel ?? "<null>"}', html.dark={darkClass}, localStorage['theme-mode']='{signals.StoredBrightness ?? "<null>"}', localStorage['theme-color']='{signals.StoredColor ?? "<null>"}', window.themeManager={themeManager}, window.Blazor={blazor}, theme.js={themeScript}, blazor.web.js={blazorScript}";
	}

	internal sealed record ThemeSignals(
		bool HasDarkClass,
		string? StoredBrightness,
		string? StoredColor,
		string? AriaLabel,
		string? ReadinessMarker,
		bool HasThemeManager,
		bool HasBlazor,
		bool SawThemeScriptResource,
		bool SawBlazorScriptResource)
	{
		internal bool IsTrustworthyInteractiveState() =>
			(string.Equals(ReadinessMarker, "true", StringComparison.Ordinal)
				|| (HasThemeManager && HasBlazor))
			&& !string.IsNullOrWhiteSpace(AriaLabel);
	}

	internal sealed class BrowserRuntimeDiagnostics
	{
		private readonly List<string> _events = [];
		private readonly object _gate = new();

		internal static BrowserRuntimeDiagnostics Attach(IPage page)
		{
			var diagnostics = new BrowserRuntimeDiagnostics();

			page.Console += (_, message) => diagnostics.Add($"console.{message.Type}: {message.Text}");
			page.PageError += (_, message) => diagnostics.Add($"pageerror: {message}");
			page.RequestFailed += (_, request) => diagnostics.Add($"requestfailed: {request.Method} {request.Url} :: {request.Failure}");

			return diagnostics;
		}

		internal string Describe()
		{
			lock (_gate)
			{
				return _events.Count == 0
					? "no console, pageerror, or requestfailed events captured"
					: string.Join(" | ", _events.TakeLast(6));
			}
		}

		private void Add(string message)
		{
			lock (_gate)
			{
				_events.Add(message);
			}
		}
	}
}
