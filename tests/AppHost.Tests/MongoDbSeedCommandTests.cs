// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     MongoDbSeedCommandTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : MyBlog
// Project Name :  AppHost.Tests
// =============================================

using System.Collections.Immutable;

using Aspire.Hosting;

using FluentAssertions;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;

namespace AppHost.Tests;

/// <summary>
/// Model-level tests for the local-only MongoDB seed-data operator command (issue #260).
/// These tests verify the Aspire resource annotation contract only — they do not start the
/// Aspire host, spin up containers, or touch a live database.
/// </summary>
public sealed class MongoDbSeedCommandTests
{
	private const string CommandName = "seed-myblog-data";

	private static Task<IDistributedApplicationTestingBuilder> CreateBuilderAsync() =>
		DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>(
			args: [],
			configureBuilder: static (options, _) => { options.DisableDashboard = true; },
			cancellationToken: TestContext.Current.CancellationToken);

	/// <summary>
	/// Acceptance criterion #1: The mongodb resource exposes a "seed-myblog-data" operator action.
	/// </summary>
	[Fact]
	public async Task MongoDb_Resource_Exposes_SeedMyBlogData_Command_Annotation()
	{
		// Arrange
		var builder = await CreateBuilderAsync();
		var mongoResource = builder.Resources.Single(static r => r.Name == "mongodb");

		// Act
		var annotation = mongoResource.Annotations
			.OfType<ResourceCommandAnnotation>()
			.SingleOrDefault(static a => a.Name == CommandName);

		// Assert
		annotation.Should().NotBeNull(
			"the mongodb resource must expose a 'seed-myblog-data' operator action per issue #260");
	}

	/// <summary>
	/// Acceptance criterion: The seed command must NOT be highlighted — it is additive,
	/// not destructive, so it should not carry a danger indicator.
	/// </summary>
	[Fact]
	public async Task SeedMyBlogData_Command_Is_Not_Highlighted()
	{
		// Arrange
		var builder = await CreateBuilderAsync();
		var annotation = GetSeedMyBlogDataAnnotation(builder);

		// Assert
		annotation.IsHighlighted.Should().BeFalse(
			"an additive seed command must set IsHighlighted = false to avoid alarming the operator");
	}

	/// <summary>
	/// Acceptance criterion: The seed command must NOT require a confirmation prompt.
	/// Seeding is non-destructive and reversible, so no y/n dialog is needed.
	/// </summary>
	[Fact]
	public async Task SeedMyBlogData_Command_Has_No_ConfirmationMessage()
	{
		// Arrange
		var builder = await CreateBuilderAsync();
		var annotation = GetSeedMyBlogDataAnnotation(builder);

		// Assert
		annotation.ConfirmationMessage.Should().BeNullOrEmpty(
			"the seed command is additive and must not display a confirmation dialog");
	}

	/// <summary>
	/// Acceptance criterion: The seed command's icon must be "DatabaseArrowUp".
	/// </summary>
	[Fact]
	public async Task SeedMyBlogData_Command_Has_DatabaseArrowUp_Icon()
	{
		// Arrange
		var builder = await CreateBuilderAsync();
		var annotation = GetSeedMyBlogDataAnnotation(builder);

		// Assert
		annotation.IconName.Should().Be("DatabaseArrowUp",
			"the seed command must use the DatabaseArrowUp icon per issue #260");
	}

	/// <summary>
	/// Acceptance criterion: The seed command is enabled only when MongoDB is healthy.
	/// </summary>
	[Fact]
	public async Task SeedMyBlogData_UpdateState_Returns_Enabled_When_MongoDB_Is_Healthy()
	{
		// Arrange
		var builder = await CreateBuilderAsync();
		var annotation = GetSeedMyBlogDataAnnotation(builder);

		var snapshot = BuildSnapshot(HealthStatus.Healthy);

		var ctx = new UpdateCommandStateContext
		{
			ResourceSnapshot = snapshot,
			ServiceProvider = new ServiceCollection().BuildServiceProvider(),
		};

		// Act
		var state = annotation.UpdateState(ctx);

		// Assert
		state.Should().Be(ResourceCommandState.Enabled,
			"the seed-myblog-data command must be available when MongoDB is healthy");
	}

	/// <summary>
	/// Acceptance criterion: The seed command must be disabled when MongoDB is not healthy
	/// to prevent inserts against an unstable or stopped container.
	/// </summary>
	[Fact]
	public async Task SeedMyBlogData_UpdateState_Returns_Disabled_When_MongoDB_Is_Unhealthy()
	{
		// Arrange
		var builder = await CreateBuilderAsync();
		var annotation = GetSeedMyBlogDataAnnotation(builder);

		var snapshot = BuildSnapshot(HealthStatus.Unhealthy);

		var ctx = new UpdateCommandStateContext
		{
			ResourceSnapshot = snapshot,
			ServiceProvider = new ServiceCollection().BuildServiceProvider(),
		};

		// Act
		var state = annotation.UpdateState(ctx);

		// Assert
		state.Should().Be(ResourceCommandState.Disabled,
			"the seed-myblog-data command must be unavailable when MongoDB is unhealthy");
	}


	// ---------------------------------------------------------------------------
	// Helpers
	// ---------------------------------------------------------------------------

	private static ResourceCommandAnnotation GetSeedMyBlogDataAnnotation(IDistributedApplicationTestingBuilder builder)
	{
		var mongoResource = builder.Resources.Single(static r => r.Name == "mongodb");

		return mongoResource.Annotations
			.OfType<ResourceCommandAnnotation>()
			.Single(static a => a.Name == CommandName);
	}

	/// <summary>
	/// Creates a <see cref="CustomResourceSnapshot"/> with the given health status.
	/// <para>
	/// <see cref="CustomResourceSnapshot.HealthReports"/> has an internal init accessor and
	/// <see cref="CustomResourceSnapshot.HealthStatus"/> is a private computed property —
	/// both are inaccessible from external assemblies via normal C#.  Reflection is required.
	/// </para>
	/// </summary>
	private static CustomResourceSnapshot BuildSnapshot(HealthStatus health)
	{
		var snapshot = new CustomResourceSnapshot
		{
			ResourceType = "MongoDB.Server",
			Properties = [],
		};

		var reports = ImmutableArray.Create(
			new HealthReportSnapshot("ready", health, null, null));

		var type = typeof(CustomResourceSnapshot);
		type
			.GetProperty("HealthReports")!
			.GetSetMethod(nonPublic: true)!
			.Invoke(snapshot, [reports]);

		type.GetProperty("HealthStatus")!
			.GetSetMethod(nonPublic: true)!
			.Invoke(snapshot, [(HealthStatus?)health]);

		return snapshot;
	}
}
