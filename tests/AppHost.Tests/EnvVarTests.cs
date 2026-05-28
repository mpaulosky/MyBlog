// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     EnvVarTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  AppHost.Tests
// =============================================

using Aspire.Hosting;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

namespace AppHost.Tests;

/// <summary>
/// Tests for environment variable resolution in the web resource.
/// </summary>
public class EnvVarTests
{
	[Fact]
	public async Task WebResourceHasMongoDbConnectionString()
	{
		// Arrange
		var appHost = await DistributedApplicationTestingBuilder
			.CreateAsync<Projects.AppHost>(
				args: [],
				configureBuilder: static (options, _) =>
				{
					options.DisableDashboard = true;
				},
				cancellationToken: TestContext.Current.CancellationToken);

		var webResource = (IResourceWithEnvironment)appHost.Resources
			.Single(static r => r.Name == "web");

		// Act
		var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish);
		var resolvedConfig = await ExecutionConfigurationBuilder
			.Create(webResource)
			.WithEnvironmentVariablesConfig()
			.BuildAsync(executionContext, NullLogger.Instance, TestContext.Current.CancellationToken);

		// Assert
		resolvedConfig.EnvironmentVariables
			.Select(kvp => kvp.Key)
			.Should().Contain("ConnectionStrings__myblog");
	}

	[Fact]
	public async Task WebResourceHasRedisConnectionString()
	{
		// Arrange
		var appHost = await DistributedApplicationTestingBuilder
			.CreateAsync<Projects.AppHost>(
				args: [],
				configureBuilder: static (options, _) =>
				{
					options.DisableDashboard = true;
				},
				cancellationToken: TestContext.Current.CancellationToken);

		var webResource = (IResourceWithEnvironment)appHost.Resources
			.Single(static r => r.Name == "web");

		// Act
		var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish);
		var resolvedConfig = await ExecutionConfigurationBuilder
			.Create(webResource)
			.WithEnvironmentVariablesConfig()
			.BuildAsync(executionContext, NullLogger.Instance, TestContext.Current.CancellationToken);

		// Assert
		resolvedConfig.EnvironmentVariables
			.Select(kvp => kvp.Key)
			.Should().Contain("ConnectionStrings__redis");
	}
}
