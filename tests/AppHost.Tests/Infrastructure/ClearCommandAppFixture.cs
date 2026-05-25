// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     ClearCommandAppFixture.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : MyBlog
// Project Name :  AppHost.Tests
// =============================================

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

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
	public IDistributedApplicationTestingBuilder Builder { get; private set; } = null!;
	public DistributedApplication App { get; private set; } = null!;

	/// <summary>
	/// Connection string for the live MongoDB container — use this to seed / verify data
	/// in integration tests.  Populated after <see cref="InitializeAsync"/> completes.
	/// </summary>
	public string MongoConnectionString { get; private set; } = string.Empty;

	public async ValueTask InitializeAsync()
	{
		// Propagate Testing mode so the web resource starts fast (in-memory fakes).
		Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

		Builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>(
			args: [],
			configureBuilder: static (options, _) => { options.DisableDashboard = true; });

		// Inject the Testing env var directly into the web resource annotation so DCP
		// uses the correct value when launching the child process.
		SetWebEnvironmentVariable(Builder, "ASPNETCORE_ENVIRONMENT", "Testing");
		SetWebEnvironmentVariable(Builder, "Auth0__Domain", string.Empty);
		SetWebEnvironmentVariable(Builder, "Auth0__ClientId", string.Empty);
		SetWebEnvironmentVariable(Builder, "Auth0__ClientSecret", string.Empty);

		App = await Builder.BuildAsync();
		await App.StartAsync();

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
