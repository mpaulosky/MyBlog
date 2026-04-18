//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     RoleClaimsHelperTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Unit.Tests
//=======================================================

// ============================================
// Copyright (c) 2025. All rights reserved.
// File Name :     RoleClaimsHelperTests.cs
// Company :       mpaulosky
// Author :        mpaulosky
// Solution Name : MyBlog
// Project Name :  Unit.Tests
// =============================================
using Microsoft.Extensions.Configuration;

using MyBlog.Web.Security;

namespace MyBlog.Unit.Tests.Security;

public class RoleClaimsHelperTests
{
	[Fact]
	public void GetRoleClaimTypes_UsesConfiguredDistinctValues_WhenPresent()
	{
		// Arrange
		var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string?>
				{
					["Auth0:RoleClaimTypes:0"] = "roles",
					["Auth0:RoleClaimTypes:1"] = "https://myblog/roles",
					["Auth0:RoleClaimTypes:2"] = "roles"
				})
				.Build();

		// Act
		var result = RoleClaimsHelper.GetRoleClaimTypes(configuration);

		// Assert
		result.Should().BeEquivalentTo(["roles", "https://myblog/roles"]);
	}

	[Fact]
	public void GetRoleClaimTypes_ReturnsDefaults_WhenConfigurationIsMissing()
	{
		// Arrange
		var configuration = new ConfigurationBuilder().Build();

		// Act
		var result = RoleClaimsHelper.GetRoleClaimTypes(configuration);

		result.Should().BeEquivalentTo(RoleClaimsHelper.DefaultRoleClaimTypes);
	}

	[Theory]
	[InlineData("Admin", new[] { "Admin" })]
	[InlineData("Admin,Author", new[] { "Admin", "Author" })]
	[InlineData(" [\"Admin\",\"Author\"] ", new[] { "Admin", "Author" })]
	public void ExpandRoleValues_ParsesSupportedFormats(string input, string[] expected)
	{
		// Arrange (none)
		// Act
		var result = RoleClaimsHelper.ExpandRoleValues(input);

		result.Should().BeEquivalentTo(expected);
	}

	[Fact]
	public void AddRoleClaims_AddsExpandedRoleClaimsWithoutDuplicates()
	{
		// Arrange
		var identity = new ClaimsIdentity(new[]
		{
						new Claim("roles", "Admin,Author"),
						new Claim(ClaimTypes.Role, "Admin")
				}, "TestAuth", ClaimTypes.Name, ClaimTypes.Role);

		// Act
		RoleClaimsHelper.AddRoleClaims(identity, ["roles"]);

		// Assert
		identity.FindAll(ClaimTypes.Role)
				.Select(claim => claim.Value)
				.Should()
				.BeEquivalentTo(["Admin", "Author"]);
	}

	[Fact]
	public void GetRoles_CollectsDistinctRolesAcrossMultipleClaimTypes()
	{
		// Arrange
		var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
		{
						new Claim(ClaimTypes.Role, "Admin"),
						new Claim("https://myblog/roles", "[\"Author\",\"Admin\"]"),
						new Claim("roles", "Editor,Author")
				}, "TestAuth", ClaimTypes.Name, ClaimTypes.Role));

		// Act
		var result = RoleClaimsHelper.GetRoles(principal);

		// Assert
		result.Should().Equal("Admin", "Author", "Editor");
	}
}
