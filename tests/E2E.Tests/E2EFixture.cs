//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     E2EFixture.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  E2E.Tests
//=======================================================

using Aspire.Hosting;

namespace MyBlog.E2E.Tests;

public sealed class E2EFixture : IAsyncLifetime
{
	public DistributedApplication App { get; private set; } = null!;

	public async Task InitializeAsync()
	{
		// Arrange — build the Aspire app host for E2E testing
		var appHost = await DistributedApplicationTestingBuilder
			.CreateAsync<Projects.AppHost>();

		App = await appHost.BuildAsync();

		// Act — start the application and wait for the web resource to be healthy
		await App.StartAsync();
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
		await App.WaitForHealthyAsync("web", cts.Token);
	}

	public async Task DisposeAsync()
	{
		if (App is not null)
			await App.DisposeAsync();
	}
}
