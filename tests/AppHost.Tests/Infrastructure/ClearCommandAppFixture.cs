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

namespace AppHost.Infrastructure;

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

		// NOTE: No DCP pre-warm here intentionally.
		// ClearCommandAppFixture starts MongoDB in a named Docker volume (mongo-data-v7).
		// Pre-warming starts the AppHost (including MongoDB), stops it, then the main start
		// also starts MongoDB — but each abrupt stop leaves the WiredTiger journal dirty.
		// Enough dirty stops across sequential collections cause MongoDB to exit with code 100
		// (unrecoverable storage engine). The retry policy with exponential backoff below is
		// sufficient to handle DCP cold-start latency without starting and killing MongoDB first.

		// DCP initialization can timeout with the hardcoded 20-second Aspire timeout on first run.
		// On slow systems (CI VMs, CachyOS), DCP consistently takes >20 seconds for cold-start.
		// The retry also covers MongoDB exit-code-100 (WiredTiger dirty journal from a previous
		// collection's abrupt stop): the 10s backoff gives the Docker daemon time to fully release
		// the named volume lock before the next startup attempt.
		var retryPolicy = new ResiliencePipelineBuilder()
			.AddRetry(new Polly.Retry.RetryStrategyOptions
			{
				MaxRetryAttempts = 3,
				Delay = TimeSpan.FromSeconds(10),  // Initial 10-second backoff
				BackoffType = DelayBackoffType.Exponential,
				UseJitter = false,
				ShouldHandle = new PredicateBuilder()
					.Handle<OperationCanceledException>()
					.Handle<Polly.Timeout.TimeoutRejectedException>(),
				OnRetry = args =>
				{
					var delay = args.RetryDelay;
					Console.Error.WriteLine(
						$"[ClearCommandAppFixture] Retry {args.AttemptNumber}/3 after {delay.TotalSeconds:F0}s — " +
						"likely DCP timeout or MongoDB WiredTiger dirty-journal recovery.");
					return default;
				}
			})
			.Build();

		await retryPolicy.ExecuteAsync(async _ =>
		{
			// Dispose any partially-started app from a previous attempt to avoid orphaned
			// Docker containers holding the WiredTiger.lock on the named volume.
			if (App is not null)
			{
				await App.StopAsync(CancellationToken.None);
				await App.DisposeAsync();
				App = null!;
				// Brief pause to allow the Docker daemon to fully release the volume lock
				// before the next container start attempts to acquire it.
				await Task.Delay(TimeSpan.FromSeconds(3), CancellationToken.None);
			}

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

			// Wait for the MongoDB container to reach Running state inside the retry boundary
			// so that a MongoDB exit-code-100 (dirty journal) is treated as a retryable failure.
			using var mongoCts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
			await App.ResourceNotifications.WaitForResourceAsync(
				"mongodb",
				KnownResourceStates.Running,
				mongoCts.Token);
		});

		using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
		MongoConnectionString = await App.GetConnectionStringAsync("mongodb", cts.Token)
			?? throw new InvalidOperationException(
				"Could not resolve the MongoDB connection string from the Aspire host.");
	}

	public async ValueTask DisposeAsync()
	{
		if (App is not null)
		{
			// Explicitly stop before dispose so MongoDB has time to flush the WiredTiger
			// journal cleanly. Without this, the next collection's fixture may find a dirty
			// journal and fail to start MongoDB (exit code 100).
			await App.StopAsync();
			await App.DisposeAsync();
		}
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
