// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ClearCommandAppFixture.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : MyBlog
// Project Name :  AppHost.Tests
// =============================================

using Aspire.Hosting;

using Polly;

namespace AppHost.Tests.Infrastructure;

/// <summary>
/// xUnit <see cref="IAsyncLifetime"/> fixture that boots the full Aspire host and waits
/// for the MongoDB container to reach the Running state before any test executes.
/// <para>
/// The "clear-myblog-data" handler captures the <c>mongo</c> resource builder in a closure
/// and resolves the connection string via <c>ConnectionStringExpression.GetValueAsync()</c>
/// — it does NOT use <c>context.ServiceProvider</c>.  A real Aspire host (with DCP port
/// allocation) is therefore required; a Testcontainers-only fixture cannot intercept it.
/// </para>
/// </summary>
public sealed class ClearCommandAppFixture : IAsyncLifetime
{
	private static bool IsCI => Environment.GetEnvironmentVariable("CI")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

	public IDistributedApplicationTestingBuilder Builder { get; private set; } = null!;
	public DistributedApplication App { get; private set; } = null!;

	/// <summary>
	/// Connection string for the live MongoDB container — use this to seed / verify data
	/// in integration tests.  Populated after <see cref="InitializeAsync"/> completes.
	/// </summary>
	public string MongoConnectionString { get; private set; } = string.Empty;

	public async ValueTask InitializeAsync()
	{
		// Skip initialization in CI where DCP startup timeout is unavoidable.
		if (IsCI)
		{
			return; // CI environment: tests will be skipped by collection definition
		}

		// Propagate Testing mode so the web resource starts fast (in-memory fakes).
		Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

		// Pre-warm DCP by attempting one quick initialization cycle.
		// This helps DCP get past initial setup and reduces cold-start latency.
		await PreWarmDcpAsync();

		// DCP initialization can timeout with the hardcoded 20-second Aspire timeout on first run.
		// On slow systems (CI VMs, CachyOS), DCP consistently takes >20 seconds for cold-start.
		// Use Polly retry with LARGE exponential backoff (10s, 20s, 40s) to allow DCP time to
		// recover between attempts. Each retry waits progressively longer before reattempting.
		var retryPolicy = new ResiliencePipelineBuilder()
			.AddRetry(new Polly.Retry.RetryStrategyOptions
			{
				MaxRetryAttempts = 3,
				Delay = TimeSpan.FromSeconds(10),  // Initial 10-second backoff
				BackoffType = DelayBackoffType.Exponential,
				UseJitter = false,
				ShouldHandle = new PredicateBuilder()
					.Handle<OperationCanceledException>()
					.Handle<Polly.Timeout.TimeoutRejectedException>()
			})
			.Build();

		await retryPolicy.ExecuteAsync(async _ =>
		{
			Builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>(
				args: [],
				configureBuilder: static (options, _) => { options.DisableDashboard = true; },
				cancellationToken: CancellationToken.None);

			// Inject the Testing env var directly into the web resource annotation so DCP
			// uses the correct value when launching the child process.
			SetWebEnvironmentVariable(Builder, "ASPNETCORE_ENVIRONMENT", "Testing");
			SetWebEnvironmentVariable(Builder, "Auth0__Domain", string.Empty);
			SetWebEnvironmentVariable(Builder, "Auth0__ClientId", string.Empty);
			SetWebEnvironmentVariable(Builder, "Auth0__ClientSecret", string.Empty);

			App = await Builder.BuildAsync(CancellationToken.None);
			await App.StartAsync(CancellationToken.None);
		});

		// Wait for the MongoDB container to be Running before tests try to seed data.
		using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
		await App.ResourceNotifications.WaitForResourceAsync(
			"mongodb",
			KnownResourceStates.Running,
			cts.Token);

		MongoConnectionString = await App.GetConnectionStringAsync("mongodb", cts.Token)
			?? throw new InvalidOperationException(
				"Could not resolve the MongoDB connection string from the Aspire host.");
	}

	/// <summary>
	/// Pre-warm DCP by attempting one quick initialization cycle.
	/// This helps DCP get past initial setup and reduces cold-start latency for main initialization.
	/// Best-effort: does not block main initialization if it fails.
	/// </summary>
	private static async Task PreWarmDcpAsync()
	{
		try
		{
			var preWarmTimeout = TimeSpan.FromSeconds(60);
			using var cts = new CancellationTokenSource(preWarmTimeout);

			var preWarmBuilder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>(
				args: [],
				configureBuilder: static (options, _) => { options.DisableDashboard = true; },
				cancellationToken: cts.Token);

			var preWarmApp = await preWarmBuilder.BuildAsync();
			await preWarmApp.StartAsync(cts.Token);
			await preWarmApp.StopAsync();
			await preWarmApp.DisposeAsync();
		}
		catch (OperationCanceledException)
		{
			// Pre-warm timed out; proceed with main initialization (will retry if needed).
		}
		catch (Exception)
		{
			// Pre-warm failed; proceed with main initialization (will retry if needed).
		}
	}

	public async ValueTask DisposeAsync()
	{
		await App.DisposeAsync();
	}

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
}
