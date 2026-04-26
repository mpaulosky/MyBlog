//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     RoleClaimsHelperTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using Microsoft.Extensions.Configuration;

using MyBlog.Web.Security;

namespace Web.Security;

public class RoleClaimsHelperTests
{
	[Fact]
	public void GetRoleClaimTypes_UsesConfiguredDistinctValues_WhenPresent()
	{
		// Arrange
		var configuration = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string?>
				{
					["Auth0:RoleClaimTypes:0"] = "https://articlesite.com/roles",
					["Auth0:RoleClaimTypes:1"] = "https://myblog/roles",
					["Auth0:RoleClaimTypes:2"] = "roles",
					["Auth0:RoleClaimTypes:3"] = "roles"
				})
				.Build();

		// Act
		var result = RoleClaimsHelper.GetRoleClaimTypes(configuration);

		// Assert
		result.Should().BeEquivalentTo(["https://articlesite.com/roles", "https://myblog/roles", "roles"]);
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

	[Theory]
	[InlineData("roles", true)]
	[InlineData("role", true)]
	[InlineData("https://articlesite.com/roles", true)]
	[InlineData("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", true)]
	[InlineData("https://articlesite.com/app_metadata", false)]
	public void IsRoleClaimType_DetectsExpectedClaimTypes(string claimType, bool expected)
	{
		// Act
		var result = RoleClaimsHelper.IsRoleClaimType(claimType);

		// Assert
		result.Should().Be(expected);
	}

	[Fact]
	public void AddRoleClaims_AddsExpandedRoleClaimsWithoutDuplicates()
	{
		// Arrange
		var identity = new ClaimsIdentity(new[]
		{
						new Claim("https://articlesite.com/roles", "Admin,Author"),
						new Claim(ClaimTypes.Role, "Admin")
				}, "TestAuth", ClaimTypes.Name, ClaimTypes.Role);

		// Act
		RoleClaimsHelper.AddRoleClaims(identity, ["https://articlesite.com/roles"]);

		// Assert
		identity.FindAll(ClaimTypes.Role)
				.Select(claim => claim.Value)
				.Should()
				.BeEquivalentTo(["Admin", "Author"]);
	}

	[Fact]
	public void AddRoleClaims_InfersNamespacedRoleClaims_WhenConfiguredTypesDoNotMatch()
	{
		// Arrange
		var identity = new ClaimsIdentity(new[]
		{
						new Claim("https://articlesite.com/roles", "[\"Admin\",\"User\"]")
				}, "TestAuth", ClaimTypes.Name, ClaimTypes.Role);

		// Act
		RoleClaimsHelper.AddRoleClaims(identity, ["https://myblog/roles"]);

		// Assert
		identity.FindAll(ClaimTypes.Role)
				.Select(claim => claim.Value)
				.Should()
				.BeEquivalentTo(["Admin", "User"]);
	}

	[Fact]
	public void GetRoles_CollectsDistinctRolesAcrossMultipleClaimTypes()
	{
		// Arrange
		var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
		{
						new Claim(ClaimTypes.Role, "Admin"),
						new Claim("https://articlesite.com/roles", "Admin"),
						new Claim("https://myblog/roles", "[\"Author\",\"Admin\"]"),
						new Claim("roles", "Editor,Author")
				}, "TestAuth", ClaimTypes.Name, ClaimTypes.Role));

		// Act
		var result = RoleClaimsHelper.GetRoles(principal);

		// Assert
		result.Should().Equal("Admin", "Author", "Editor");
	}

	[Fact]
	public void GetRoles_IncludesNamespacedRoleClaims_WhenRoleClaimTypeWasNotConfigured()
	{
		// Arrange
		var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
		{
						new Claim("https://articlesite.com/roles", "Admin"),
						new Claim("https://articlesite.com/roles", "User")
				}, "TestAuth", ClaimTypes.Name, ClaimTypes.Role));

		// Act
		var result = RoleClaimsHelper.GetRoles(principal, ["https://myblog/roles"]);

		// Assert
		result.Should().Equal("Admin", "User");
	}

	[Fact]
	public void GetRoles_ReturnsEmpty_WhenUserHasNoClaims()
	{
		// Arrange
		var principal = new ClaimsPrincipal(new ClaimsIdentity([], "TestAuth", ClaimTypes.Name, ClaimTypes.Role));

		// Act
		var result = RoleClaimsHelper.GetRoles(principal);

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public void GetRoles_ReturnsRoles_FromAuth0NamespacedClaim()
	{
		// Arrange
		var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
		{
						new Claim("https://myblog/roles", "[\"Admin\",\"Author\"]")
				}, "TestAuth", ClaimTypes.Name, ClaimTypes.Role));

		// Act
		var result = RoleClaimsHelper.GetRoles(principal);

		// Assert
		result.Should().Equal("Admin", "Author");
	}

	[Fact]
	public void GetRoles_ReturnsRoles_FromStandardRoleClaim()
	{
		// Arrange
		var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
		{
						new Claim(ClaimTypes.Role, "Admin"),
						new Claim(ClaimTypes.Role, "Editor")
				}, "TestAuth", ClaimTypes.Name, ClaimTypes.Role));

		// Act
		var result = RoleClaimsHelper.GetRoles(principal);

		// Assert
		result.Should().Equal("Admin", "Editor");
	}

	[Fact]
	public void GetRoles_IgnoresNonRoleClaims_WhenMixedClaimsPresent()
	{
		// Arrange
		var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
		{
						new Claim(ClaimTypes.Role, "Admin"),
						new Claim(ClaimTypes.Email, "user@example.com"),
						new Claim(ClaimTypes.Name, "Test User"),
						new Claim("department", "Engineering")
				}, "TestAuth", ClaimTypes.Name, ClaimTypes.Role));

		// Act
		var result = RoleClaimsHelper.GetRoles(principal);

		// Assert
		result.Should().ContainSingle().Which.Should().Be("Admin");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void ExpandRoleValues_ReturnsEmpty_WhenInputIsNullOrWhitespace(string? input)
	{
		// Arrange (none)
		// Act
		var result = RoleClaimsHelper.ExpandRoleValues(input);

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public void ExpandRoleValues_ReturnsEmpty_WhenJsonIsInvalid()
	{
		// Arrange (none)
		// Act
		var result = RoleClaimsHelper.ExpandRoleValues("[not-valid-json");

		// Assert
		result.Should().BeEmpty();
	}
}
