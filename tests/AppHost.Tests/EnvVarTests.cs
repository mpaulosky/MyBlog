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
#pragma warning disable CS0618 // Type or member is obsolete
		var envVars = await webResource.GetEnvironmentVariableValuesAsync(
			DistributedApplicationOperation.Publish);
#pragma warning restore CS0618 // Type or member is obsolete

		// Assert
		envVars.Should().ContainKey("ConnectionStrings__myblog");
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
#pragma warning disable CS0618 // Type or member is obsolete
		var envVars = await webResource.GetEnvironmentVariableValuesAsync(
			DistributedApplicationOperation.Publish);
#pragma warning restore CS0618 // Type or member is obsolete

		// Assert
		envVars.Should().ContainKey("ConnectionStrings__redis");
	}
}
