//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     E2EFixture.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  E2E.Tests
//=======================================================

namespace Tests.E2E;

public sealed class E2EFixture : IAsyncLifetime
{
	public DistributedApplication App { get; private set; } = null!;

	public async ValueTask InitializeAsync()
	{
		// Arrange — build the Aspire app host for E2E testing
		var appHost = await DistributedApplicationTestingBuilder
			.CreateAsync<Projects.AppHost>();

		App = await appHost.BuildAsync();

		// Act — start the application and wait for the web resource to be healthy
		await App.StartAsync();
		await App.WaitForHealthyAsync("web");
	}

	public async ValueTask DisposeAsync()
	{
		await App.DisposeAsync();
	}
}
