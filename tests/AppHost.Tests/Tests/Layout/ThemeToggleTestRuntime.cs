//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     ThemeToggleTestRuntime.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  AppHost.Tests
//=======================================================

namespace AppHost.Tests.Tests.Layout;

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

	internal static async Task<ThemeStateWaitResult> WaitForThemeStateAsync(
			IPage page,
			ILocator toggleButton,
			string expectedBrightness,
			bool expectedDarkClass,
			TimeSpan? timeout = null)
	{
		var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(10));
		ThemeSignals? lastSignals = null;

		while (DateTime.UtcNow < deadline)
		{
			lastSignals = await ReadThemeSignalsAsync(page, toggleButton);
			if (lastSignals.IsTrustworthyInteractiveState()
					&& lastSignals.HasDarkClass == expectedDarkClass
					&& string.Equals(lastSignals.StoredBrightness, expectedBrightness, StringComparison.Ordinal)
					&& (lastSignals.AriaLabel?.Contains($"currently {expectedBrightness}", StringComparison.Ordinal) ?? false))
			{
				return new(lastSignals, true);
			}

			await Task.Delay(250);
		}

		lastSignals ??= await ReadThemeSignalsAsync(page, toggleButton);

		return new(lastSignals, false);
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

	internal sealed record ThemeStateWaitResult(ThemeSignals Signals, bool MatchedExpectedState);

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
