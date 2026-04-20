//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     ProfileTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Unit.Tests
//=======================================================

using Microsoft.AspNetCore.Components.Authorization;

using MyBlog.Web.Features.UserManagement;

namespace MyBlog.Unit.Tests.Features.UserManagement;

public class ProfileTests : BunitContext
{
	[Fact]
	public void Profile_RendersIdentityDetailsRolesPictureAndClaims()
	{
		// Arrange
		var principal = CreatePrincipal(
				name: "Admin User",
				email: "admin@example.com",
				userId: "auth0|123",
				pictureUrl: "https://example.com/avatar.png",
				rolesJson: "[\"Admin\",\"Author\"]",
				extraClaims: [new Claim("department", "Engineering")]);

		// Act
		var cut = RenderForUser(principal);

		// Assert
		cut.Markup.Should().Contain("Admin User");
		cut.Markup.Should().Contain("admin@example.com");
		cut.Markup.Should().Contain("auth0|123");
		cut.Markup.Should().Contain("avatar.png");
		cut.Markup.Should().Contain("Admin");
		cut.Markup.Should().Contain("Author");
		cut.Markup.Should().Contain("department");
		cut.Markup.Should().Contain("Engineering");
	}

	[Fact]
	public void Profile_UsesFallbackValues_WhenOptionalClaimsAreMissing()
	{
		// Arrange
		var principal = CreatePrincipal(
				name: null,
				email: null,
				userId: null,
				pictureUrl: null,
				rolesJson: null,
				extraClaims: []);

		// Act
		var cut = RenderForUser(principal);

		// Assert
		cut.Markup.Should().Contain("Unknown User");
		cut.Markup.Should().Contain("No email claim found");
		cut.Markup.Should().Contain("No roles found in the current claims.");
		cut.Markup.Should().Contain(">NE<");
	}

	private IRenderedComponent<Profile> RenderForUser(ClaimsPrincipal principal)
	{
		return Render<Profile>(parameters => parameters
				.AddCascadingValue(Task.FromResult(new AuthenticationState(principal))));
	}

	private static ClaimsPrincipal CreatePrincipal(
			string? name,
			string? email,
			string? userId,
			string? pictureUrl,
			string? rolesJson,
			IEnumerable<Claim> extraClaims)
	{
		var claims = new List<Claim>();

		if (!string.IsNullOrWhiteSpace(name))
		{
			claims.Add(new Claim(ClaimTypes.Name, name));
		}

		if (!string.IsNullOrWhiteSpace(email))
		{
			claims.Add(new Claim(ClaimTypes.Email, email));
		}

		if (!string.IsNullOrWhiteSpace(userId))
		{
			claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
		}

		if (!string.IsNullOrWhiteSpace(pictureUrl))
		{
			claims.Add(new Claim("picture", pictureUrl));
		}

		if (!string.IsNullOrWhiteSpace(rolesJson))
		{
			claims.Add(new Claim("https://articlesite.com/roles", rolesJson));
		}

		claims.AddRange(extraClaims);

		return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, ClaimTypes.Role));
	}
}
