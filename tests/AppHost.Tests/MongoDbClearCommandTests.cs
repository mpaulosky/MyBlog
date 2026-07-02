// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     MongoDbClearCommandTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : MyBlog
// Project Name :  AppHost.Tests
// =============================================

using System.Collections.Immutable;

using FluentAssertions;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AppHost.Tests;

/// <summary>
/// Model-level tests for the local-only MongoDB clear-data operator command (issue #247).
/// These tests verify the Aspire resource annotation contract only — they do not start the
/// Aspire host, spin up containers, or touch a live database (except test 6).
/// </summary>
public sealed class MongoDbClearCommandTests
{
	private const string CommandName = "clear-myblog-data";

	private static Task<IDistributedApplicationTestingBuilder> CreateBuilderAsync() =>
	DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>(
	args: [],
	configureBuilder: static (options, _) => { options.DisableDashboard = true; },
	cancellationToken: TestContext.Current.CancellationToken);

	/// <summary>
	/// Acceptance criterion #1: The mongodb resource exposes a "clear-myblog-data" operator action.
	/// </summary>
	[Fact]
	public async Task MongoDb_Resource_Exposes_ClearMyBlogData_Command_Annotation()
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
		"the mongodb resource must expose a 'clear-myblog-data' operator action per issue #247");
	}

	/// <summary>
	/// Acceptance criterion #2: The clear-myblog-data action must be marked as destructive so the
	/// Aspire dashboard renders it with a danger indicator.
	/// </summary>
	[Fact]
	public async Task ClearMyBlogData_Command_IsHighlighted_Marks_It_As_Destructive()
	{
		// Arrange
		var builder = await CreateBuilderAsync();
		var annotation = GetClearMyBlogDataAnnotation(builder);

		// Assert
		annotation.IsHighlighted.Should().BeTrue(
		"a destructive data-clearing command must set IsHighlighted = true so the Aspire dashboard warns the operator");
	}

	/// <summary>
	/// Acceptance criterion #3: The action must require explicit y/n confirmation.
	/// <para>
	/// Setting <c>ConfirmationMessage</c> on the annotation causes the Aspire dashboard to render
	/// an OK/Cancel dialog before the execute callback is ever invoked.  When the operator clicks
	/// Cancel the callback is NOT called — this is the framework-managed no-op guarantee for a
	/// declined confirmation.
	/// </para>
	/// </summary>
	[Fact]
	public async Task ClearMyBlogData_Command_Has_ConfirmationMessage_Enabling_Yn_Prompt()
	{
		// Arrange
		var builder = await CreateBuilderAsync();
		var annotation = GetClearMyBlogDataAnnotation(builder);

		// Assert
		annotation.ConfirmationMessage.Should().NotBeNullOrEmpty(
		"the command must define a ConfirmationMessage so Aspire shows a y/n dialog; "
		+ "declining that dialog is the built-in no-op guarantee");
	}

	/// <summary>
	/// Acceptance criterion #4: The action is enabled only when MongoDB is healthy.
	/// </summary>
	[Fact]
	public async Task ClearMyBlogData_UpdateState_Returns_Enabled_When_MongoDB_Is_Healthy()
	{
		// Arrange
		var builder = await CreateBuilderAsync();
		var annotation = GetClearMyBlogDataAnnotation(builder);

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
		"the clear-myblog-data command must be available when MongoDB is healthy");
	}

	/// <summary>
	/// Acceptance criterion #4 (corollary): The action must be disabled when MongoDB is not healthy
	/// to prevent destructive operations on an unstable or stopped container.
	/// </summary>
	[Fact]
	public async Task ClearMyBlogData_UpdateState_Returns_Disabled_When_MongoDB_Is_Unhealthy()
	{
		// Arrange
		var builder = await CreateBuilderAsync();
		var annotation = GetClearMyBlogDataAnnotation(builder);

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
		"the clear-myblog-data command must be unavailable when MongoDB is unhealthy");
	}


	// ---------------------------------------------------------------------------
	// Helpers
	// ---------------------------------------------------------------------------

	private static ResourceCommandAnnotation GetClearMyBlogDataAnnotation(IDistributedApplicationTestingBuilder builder)
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
