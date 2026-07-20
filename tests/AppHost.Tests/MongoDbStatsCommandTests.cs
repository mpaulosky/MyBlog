// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     MongoDbStatsCommandTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : MyBlog
// Project Name :  AppHost.Tests
// =============================================

using System.Collections.Immutable;

using FluentAssertions;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AppHost;

/// <summary>
/// Model-level tests for the local-only MongoDB show-stats operator command (issue #261).
/// These tests verify the Aspire resource annotation contract only — they do not start the
/// Aspire host, spin up containers, or touch a live database.
/// </summary>
public sealed class MongoDbStatsCommandTests
{
	private const string CommandName = "show-myblog-stats";

	private static Task<IDistributedApplicationTestingBuilder> CreateBuilderAsync() =>
		DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>(
			args: [],
			configureBuilder: static (options, _) => { options.DisableDashboard = true; },
			cancellationToken: TestContext.Current.CancellationToken);

	/// <summary>
	/// Acceptance criterion #1: The mongodb resource exposes a "show-myblog-stats" operator action.
	/// </summary>
	[Fact]
	public async Task MongoDb_Resource_Exposes_ShowMyBlogStats_Command_Annotation()
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
			"the mongodb resource must expose a 'show-myblog-stats' operator action per issue #261");
	}

	/// <summary>
	/// Acceptance criterion: The show-stats command must NOT be highlighted — it is read-only
	/// and should not carry a danger indicator.
	/// </summary>
	[Fact]
	public async Task ShowMyBlogStats_Command_Is_Not_Highlighted()
	{
		// Arrange
		var builder = await CreateBuilderAsync();
		var annotation = GetAnnotation(builder);

		// Assert
		annotation.IsHighlighted.Should().BeFalse(
			"a read-only stats command must set IsHighlighted = false to avoid alarming the operator");
	}

	/// <summary>
	/// Acceptance criterion: The show-stats command must NOT require a confirmation prompt.
	/// It is a read-only query and needs no y/n dialog.
	/// </summary>
	[Fact]
	public async Task ShowMyBlogStats_Command_Has_No_ConfirmationMessage()
	{
		// Arrange
		var builder = await CreateBuilderAsync();
		var annotation = GetAnnotation(builder);

		// Assert
		annotation.ConfirmationMessage.Should().BeNullOrEmpty(
			"the stats command is read-only and must not display a confirmation dialog");
	}

	/// <summary>
	/// Acceptance criterion: The show-stats command is enabled only when MongoDB is healthy.
	/// </summary>
	[Fact]
	public async Task ShowMyBlogStats_UpdateState_Returns_Enabled_When_MongoDB_Is_Healthy()
	{
		// Arrange
		var builder = await CreateBuilderAsync();
		var annotation = GetAnnotation(builder);

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
			"the show-myblog-stats command must be available when MongoDB is healthy");
	}

	/// <summary>
	/// Acceptance criterion: The show-stats command must be disabled when MongoDB is not healthy
	/// to prevent queries against an unstable or stopped container.
	/// </summary>
	[Fact]
	public async Task ShowMyBlogStats_UpdateState_Returns_Disabled_When_MongoDB_Is_Unhealthy()
	{
		// Arrange
		var builder = await CreateBuilderAsync();
		var annotation = GetAnnotation(builder);

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
			"the show-myblog-stats command must be unavailable when MongoDB is unhealthy");
	}


	// ---------------------------------------------------------------------------
	// Helpers
	// ---------------------------------------------------------------------------

	private static ResourceCommandAnnotation GetAnnotation(IDistributedApplicationTestingBuilder builder)
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
