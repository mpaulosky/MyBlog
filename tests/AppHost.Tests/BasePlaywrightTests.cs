// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     BasePlaywrightTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  AppHost.Tests
// =============================================

using AppHost.Tests.Infrastructure;
using Microsoft.Playwright;

namespace AppHost.Tests;

/// <summary>
/// Base class for Playwright tests, providing common functionality and setup for Playwright testing with ASP.NET Core.
/// All derived classes share a single <see cref="AspireManager"/> instance via the
/// <see cref="AppHostTestCollection"/> collection fixture — AppHost starts once per test run.
/// </summary>
[Collection(AppHostTestCollection.Name)]
public abstract class BasePlaywrightTests : IAsyncDisposable
{

	protected BasePlaywrightTests(AspireManager aspireManager) =>
		AspireManager = aspireManager ?? throw new ArgumentNullException(nameof(aspireManager));

	AspireManager AspireManager { get; }
	PlaywrightManager PlaywrightManager => AspireManager.PlaywrightManager;

	// CI cold-start can take up to 2 min; local dev is typically ~10 s
	private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(120);

	private readonly List<IBrowserContext> _contexts = new();

	/// <summary>
	/// Runs <paramref name="test"/> against a fresh anonymous browser context.
	/// </summary>
	protected async Task InteractWithPageAsync(
		string serviceName,
		Func<IPage, Task> test,
		ViewportSize? size = null)
	{
		var endpoint = GetEndpoint(serviceName);
		await WaitForWebReadyAsync(endpoint, DefaultTimeout);

		var page = await CreatePageAsync(endpoint, size);
		try
		{
			await test(page);
		}
		finally
		{
			await page.CloseAsync();
		}
	}

	/// <summary>
	/// Runs <paramref name="test"/> against a browser context authenticated as a standard User.
	/// Authentication is performed via the Testing environment's <c>/test/login?role=user</c>
	/// endpoint — no Auth0 or external dependency required.
	/// </summary>
	protected Task InteractWithAuthenticatedPageAsync(
		string serviceName,
		Func<IPage, Task> test,
		ViewportSize? size = null) =>
		InteractWithRolePageAsync(serviceName, test, "user", size);

	/// <summary>
	/// Runs <paramref name="test"/> against a browser context authenticated as an Admin.
	/// Authentication is performed via the Testing environment's <c>/test/login?role=admin</c>
	/// endpoint — no Auth0 or external dependency required.
	/// </summary>
	protected Task InteractWithAdminPageAsync(
		string serviceName,
		Func<IPage, Task> test,
		ViewportSize? size = null) =>
		InteractWithRolePageAsync(serviceName, test, "admin", size);

	private async Task InteractWithRolePageAsync(
		string serviceName,
		Func<IPage, Task> test,
		string role,
		ViewportSize? size = null)
	{
		var endpoint = GetEndpoint(serviceName);
		await WaitForWebReadyAsync(endpoint, DefaultTimeout);

		var page = await CreatePageAsync(endpoint, size);

		// Authenticate by navigating to the test login endpoint.
		// The web app (in Testing mode) sets a cookie-auth session with the requested role claims.
		await page.GotoAsync($"/test/login?role={role}");

		try
		{
			await test(page);
		}
		finally
		{
			await page.CloseAsync();
		}
	}

	private Uri GetEndpoint(string serviceName) =>
		AspireManager.App?.GetEndpoint(serviceName, "https")
			?? throw new InvalidOperationException($"Service '{serviceName}' not found in the application endpoints.");

	private async Task<IPage> CreatePageAsync(Uri uri, ViewportSize? size = null)
	{
		var context = await PlaywrightManager.Browser.NewContextAsync(new BrowserNewContextOptions
		{
			IgnoreHTTPSErrors = true,
			ColorScheme = ColorScheme.Dark,
			ViewportSize = size,
			BaseURL = uri.ToString()
		});

		_contexts.Add(context);
		return await context.NewPageAsync();
	}

	/// <summary>
	/// Polls <c>/alive</c> on the given endpoint until it returns 2xx or the timeout elapses.
	/// Uses a certificate-ignoring handler so that self-signed HTTPS certs in CI don't block startup.
	/// Using /alive (not /health) prevents flaky failures when Redis/MongoDB are slow to start in CI — the Testing environment uses in-memory fakes and doesn't need those dependencies.
	/// </summary>
	private static async Task WaitForWebReadyAsync(Uri endpoint, TimeSpan timeout)
	{
		using var handler = new HttpClientHandler
		{
			ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
		};
		using var client = new HttpClient(handler) { BaseAddress = endpoint };

		using var cts = new CancellationTokenSource(timeout);

		try
		{
			while (!cts.Token.IsCancellationRequested)
			{
				try
				{
					var response = await client.GetAsync("/alive", cts.Token);
					if (response.IsSuccessStatusCode)
						return;
				}
				catch (Exception) when (!cts.Token.IsCancellationRequested)
				{
					// Connection refused / SSL error during startup — keep polling
				}

				await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
			}
		}
		catch (OperationCanceledException)
		{
			// timeout fired — fall through to TimeoutException below
		}

		throw new TimeoutException($"Web app at {endpoint} was not ready after {timeout.TotalSeconds}s");
	}

	public async ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);

		foreach (var context in _contexts)
			await context.DisposeAsync();
	}
}



