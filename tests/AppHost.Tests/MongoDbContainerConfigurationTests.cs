// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     MongoDbContainerConfigurationTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : MyBlog
// Project Name :  AppHost.Tests
// =============================================

using System.Text.RegularExpressions;

using Aspire.Hosting;

using FluentAssertions;

namespace AppHost.Tests;

/// <summary>
/// Regression coverage for MongoDB AppHost container configuration.
/// </summary>
public sealed class MongoDbContainerConfigurationTests
{
	private const string AspireMongoRegistry = "docker.io";
	private const string AspireMongoImage = "library/mongo";
	private const string PinnedStableMongoTag = "7";
	private const string KnownCrashingAspireMongoTag = "8.2";
	private const string LegacyMongoDataVolume = "mongo-data";
	private const string PinnedMongoDataVolume = "mongo-data-v7";
	private const string MongoDataTargetPath = "/data/db";
	private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

	private static Task<IDistributedApplicationTestingBuilder> CreateBuilderAsync() =>
		DistributedApplicationTestingBuilder.CreateAsync<Projects.AppHost>(
			args: [],
			configureBuilder: static (options, _) => { options.DisableDashboard = true; },
			cancellationToken: TestContext.Current.CancellationToken);

	[Fact]
	public async Task MongoDb_Resource_Overrides_Known_Crashing_Aspire_Default_Image_Tag()
	{
		// Arrange
		var builder = await CreateBuilderAsync();
		var mongoResource = builder.Resources.Single(static resource => resource.Name == "mongodb");

		// Act
		var image = mongoResource.Annotations
			.OfType<ContainerImageAnnotation>()
			.Single();

		// Assert
		image.Registry.Should().Be(AspireMongoRegistry);
		image.Image.Should().Be(AspireMongoImage);
		image.Tag.Should().Be(PinnedStableMongoTag,
			$"issue #345 observed Aspire's default '{KnownCrashingAspireMongoTag}' MongoDB image exiting with code 139, so AppHost should stay pinned to the stable '{PinnedStableMongoTag}' tag");
	}

	[Fact]
	public void AppHost_Source_Pins_MongoDb_Image_Away_From_The_Known_Crashing_Default()
	{
		// Arrange
		var appHostSource = ReadRepoFile("src/AppHost/AppHost.cs");

		// Act
		var overridesMongoImageTag =
			Regex.IsMatch(
				appHostSource,
				$@"AddMongoDB\(""mongodb""\)[\s\S]*?\.WithImageTag\s*\(\s*""{Regex.Escape(PinnedStableMongoTag)}""\s*\)",
				RegexOptions.CultureInvariant)
			|| Regex.IsMatch(
				appHostSource,
				@"AddMongoDB\(""mongodb""\)[\s\S]*?\.WithImage\s*\(",
				RegexOptions.CultureInvariant);

		// Assert
		overridesMongoImageTag.Should().BeTrue(
			$"issue #345 observed the default mongo:{KnownCrashingAspireMongoTag} Aspire image crashing with exit code 139, so AppHost must explicitly pin MongoDB to the stable '{PinnedStableMongoTag}' tag");
	}

	[Fact]
	public async Task MongoDb_Resource_Uses_A_New_Data_Volume_For_The_Pinned_MongoDb_7_Image()
	{
		// Arrange
		var builder = await CreateBuilderAsync();
		var mongoResource = builder.Resources.Single(static resource => resource.Name == "mongodb");

		// Act
		var foundMounts = mongoResource.TryGetContainerMounts(out var mounts);
		var dataMount = mounts?
			.SingleOrDefault(static mount => mount.Target == MongoDataTargetPath);

		// Assert
		foundMounts.Should().BeTrue("the MongoDB resource should expose its data volume mount");
		dataMount.Should().NotBeNull("MongoDB should mount a named volume at /data/db");
		dataMount!.Type.Should().Be(ContainerMountType.Volume,
			"the AppHost should continue using a named Docker volume for MongoDB data");
		dataMount.Source.Should().Be(PinnedMongoDataVolume,
			$"the pinned MongoDB {PinnedStableMongoTag} container must not reuse the legacy '{LegacyMongoDataVolume}' volume that carries MongoDB {KnownCrashingAspireMongoTag} feature compatibility metadata");
		dataMount.Source.Should().NotBe(LegacyMongoDataVolume,
			$"reusing '{LegacyMongoDataVolume}' lets MongoDB {KnownCrashingAspireMongoTag} feature compatibility metadata crash the pinned MongoDB {PinnedStableMongoTag} container with code 62");
	}

	[Fact]
	public void AppHost_Source_Pins_MongoDb_7_To_Its_Own_Data_Volume_Name()
	{
		// Arrange
		var appHostSource = ReadRepoFile("src/AppHost/AppHost.cs");

		// Act
		var usesPinnedVolumeName = Regex.IsMatch(
			appHostSource,
			$@"AddMongoDB\(""mongodb""\)[\s\S]*?\.WithImageTag\s*\(\s*""{Regex.Escape(PinnedStableMongoTag)}""\s*\)[\s\S]*?\.WithDataVolume\s*\(\s*""{Regex.Escape(PinnedMongoDataVolume)}""\s*\)",
			RegexOptions.CultureInvariant);

		// Assert
		usesPinnedVolumeName.Should().BeTrue(
			$"the pinned MongoDB {PinnedStableMongoTag} AppHost configuration must use '{PinnedMongoDataVolume}' instead of reusing the legacy '{LegacyMongoDataVolume}' volume");
		appHostSource.Should().NotContain($".WithDataVolume(\"{LegacyMongoDataVolume}\")",
			$"reusing the legacy '{LegacyMongoDataVolume}' volume allows MongoDB {KnownCrashingAspireMongoTag} feature compatibility metadata to break the pinned MongoDB {PinnedStableMongoTag} container");
	}

	private static string ReadRepoFile(string relativePath)
	{
		return File.ReadAllText(Path.Combine(RepoRoot, relativePath));
	}
}
