// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AspireManager.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  AppHost.Tests
// =============================================

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace AppHost.Tests.Infrastructure;

/// <summary>
/// Startup and configure the Aspire application for testing.
/// </summary>
public class AspireManager : IAsyncLifetime
{
	private readonly ILogger<AspireManager> _logger = LoggerFactory.Create(builder => builder.AddConsole())
		.CreateLogger<AspireManager>();

	internal PlaywrightManager PlaywrightManager { get; } = new();

	internal DistributedApplication? App { get; private set; }

	/// <summary>
	/// Starts the <see cref="Projects.AppHost"/> Aspire application.  Called once by
	/// <see cref="InitializeAsync"/> before any test in the collection executes.
	/// </summary>
	private async Task StartAppAsync()
	{
		_logger.LogInformation("Starting AppHost Aspire application...");

		// Propagate ASPNETCORE_ENVIRONMENT=Testing to all Aspire-launched child processes.
		// In Testing mode the web app uses in-memory fake repositories, Cookie auth, and
		// skips background DB services — making E2E tests fast and self-contained.
		_logger.LogInformation("Setting ASPNETCORE_ENVIRONMENT=Testing");
		Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

		_logger.LogInformation("Creating DistributedApplicationTestingBuilder...");
		var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>(
				args: [],
				configureBuilder: static (options, _) =>
				{
					options.DisableDashboard = true;
				});

		_logger.LogInformation("Builder created successfully. Configuring...");
		builder.Configuration["ASPIRE_ALLOW_UNSECURED_TRANSPORT"] = "true";

		// Explicitly inject ASPNETCORE_ENVIRONMENT=Testing into the web resource via
		// EnvironmentCallbackAnnotation. Setting it on the parent process alone is not
		// sufficient — Aspire DCP may override ASPNETCORE_ENVIRONMENT based on its own
		// EnvironmentName when launching child processes. The annotation guarantees the
		// value is applied at subprocess launch time, after DCP finishes its own setup.
		_logger.LogInformation("Injecting ASPNETCORE_ENVIRONMENT=Testing into web resource...");
		SetWebEnvironmentVariable(builder, "ASPNETCORE_ENVIRONMENT", "Testing");

		// Fix the web project's HTTPS port so the test base URL is predictable.
		_logger.LogInformation("Fixing web endpoint port to 7043...");
		FixWebEndpointPort(builder, "https", 7043);

		_logger.LogInformation("Building Aspire application...");
		App = await builder.BuildAsync();
		_logger.LogInformation("Aspire application built successfully");

		_logger.LogInformation("Starting Aspire application services...");
		await App.StartAsync();
		_logger.LogInformation("Aspire application started successfully");

		// Wait for the web process to be alive before tests run.
		// Uses /alive (not /health) to avoid blocking on Redis/MongoDB in CI.
		// CI cold-start can take up to 3 min; local dev is typically ~10 s.
		_logger.LogInformation("Waiting for web app to become healthy...");
		await WaitForWebHealthyAsync(TimeSpan.FromSeconds(180));
		_logger.LogInformation("Web app is healthy and ready for tests");
	}

	/// <summary>
	/// Polls the web service's /alive endpoint until it returns 2xx or the timeout elapses.
	/// Uses a certificate-ignoring handler so that self-signed HTTPS certs in CI don't block startup.
	/// Using /alive (not /health) avoids waiting for Redis/MongoDB — the Testing environment uses in-memory fakes.
	/// </summary>
	private async Task WaitForWebHealthyAsync(TimeSpan timeout)
	{
		if (App is null)
		{
			_logger.LogWarning("App is null, cannot wait for health");
			return;
		}

		Uri? endpoint;
		try
		{
			endpoint = App.GetEndpoint("web", "https");
			_logger.LogInformation("Web endpoint discovered: {Endpoint}", endpoint);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to get web endpoint");
			return;
		}

		using var handler = new HttpClientHandler
		{
			ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
		};
		using var client = new HttpClient(handler) { BaseAddress = endpoint };
		using var cts = new CancellationTokenSource(timeout);

		var startTime = DateTime.UtcNow;
		int attemptCount = 0;

		try
		{
			while (!cts.Token.IsCancellationRequested)
			{
				attemptCount++;
				var elapsed = DateTime.UtcNow - startTime;
				
				try
				{
					_logger.LogInformation("Attempt {AttemptCount} (elapsed {ElapsedSeconds:F1}s): Polling {Endpoint}/alive", 
						attemptCount, elapsed.TotalSeconds, endpoint);
					
					var response = await client.GetAsync("/alive", cts.Token);
					
					_logger.LogInformation("Response status: {StatusCode}", response.StatusCode);
					
					if (response.IsSuccessStatusCode)
					{
						_logger.LogInformation("Web app is healthy! Status: {StatusCode}", response.StatusCode);
						return;
					}
					
					_logger.LogWarning("Received non-success status code: {StatusCode}. Will retry...", response.StatusCode);
				}
				catch (HttpRequestException ex)
				{
					_logger.LogWarning(ex, "HTTP request failed on attempt {AttemptCount}. Will retry...", attemptCount);
				}
				catch (OperationCanceledException) when (!cts.Token.IsCancellationRequested)
				{
					_logger.LogWarning("Request timeout during attempt {AttemptCount}. Will retry...", attemptCount);
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Unexpected error on attempt {AttemptCount}. Will retry...", attemptCount);
				}

				await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
			}
		}
		catch (OperationCanceledException)
		{
			// timeout fired — fall through to TimeoutException below
			_logger.LogError("Timeout after {AttemptCount} attempts over {TimeoutSeconds}s", attemptCount, timeout.TotalSeconds);
		}

		throw new TimeoutException($"Web app at {endpoint} was not ready after {timeout.TotalSeconds}s ({attemptCount} attempts)");
	}
	/// Adds an <see cref="EnvironmentCallbackAnnotation"/> to the named web resource so
	/// that <paramref name="key"/> is set to <paramref name="value"/> when Aspire DCP
	/// launches the child process.  This takes effect AFTER DCP injects its own
	/// environment variables, ensuring our value wins over DCP defaults.
	/// </summary>
	private static void SetWebEnvironmentVariable(
		IDistributedApplicationTestingBuilder builder,
		string key,
		string value)
	{
		var webResource = builder.Resources
			.OfType<IResourceWithEnvironment>()
			.FirstOrDefault(r => r.Name == "web");

		if (webResource is null) return;

		webResource.Annotations.Add(new EnvironmentCallbackAnnotation(
			ctx => ctx.EnvironmentVariables[key] = value));
	}

	/// <summary>
	/// Forces a fixed port on the named endpoint of the "web" resource so that the
	/// Auth0 <c>redirect_uri</c> is predictable across test runs.
	/// Setting <c>IsProxied = false</c> makes the app bind directly to <paramref name="port"/>
	/// without a DCP proxy in between, so both Playwright and the OIDC middleware see
	/// the same URL (<c>https://localhost:7043/callback</c>).
	/// </summary>
	private static void FixWebEndpointPort(IDistributedApplicationTestingBuilder builder, string scheme, int port)
	{
		var webResource = builder.Resources
			.OfType<IResourceWithEndpoints>()
			.FirstOrDefault(r => r.Name == "web");
		if (webResource is null) return;

		var endpoint = webResource.Annotations
			.OfType<EndpointAnnotation>()
			.FirstOrDefault(e => string.Equals(e.UriScheme, scheme, StringComparison.OrdinalIgnoreCase));
		if (endpoint is not null)
		{
			endpoint.Port = port;
			endpoint.IsProxied = false; // bind directly — no DCP proxy, app sees port 7043
		}
	}


	public async Task InitializeAsync()
	{
		await PlaywrightManager.InitializeAsync();
		await StartAppAsync();
	}
	public async Task DisposeAsync()
	{
		await PlaywrightManager.DisposeAsync();

		await (App?.DisposeAsync() ?? ValueTask.CompletedTask);
	}
}
