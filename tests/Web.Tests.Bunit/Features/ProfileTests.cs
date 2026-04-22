//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     ProfileTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Unit.Tests
//=======================================================

using MyBlog.Web.Features.UserManagement;

namespace Web.Features;

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

	[Fact]
	public void Profile_AdminRoleBadge_HasRedColorClasses()
	{
		// Arrange
		var principal = CreatePrincipal(
				name: "Admin User",
				email: "admin@example.com",
				userId: "auth0|admin",
				pictureUrl: null,
				rolesJson: null,
				extraClaims: [new Claim(ClaimTypes.Role, "Admin")]);

		// Act
		var cut = RenderForUser(principal);

		// Assert — the roles card Admin span must carry red Tailwind classes
		var adminBadge = cut.FindAll("span")
				.FirstOrDefault(span =>
						span.TextContent.Trim() == "Admin"
						&& span.GetAttribute("class") is { } cls
						&& cls.Contains("bg-red-100"));

		adminBadge.Should().NotBeNull("Admin role should render with red-100 background");
		adminBadge!.GetAttribute("class").Should().Contain("text-red-800");
	}

	[Fact]
	public void Profile_NonAdminRoleBadge_HasGreenColorClasses()
	{
		// Arrange
		var principal = CreatePrincipal(
				name: "Author User",
				email: "author@example.com",
				userId: "auth0|author",
				pictureUrl: null,
				rolesJson: null,
				extraClaims: [new Claim(ClaimTypes.Role, "Author")]);

		// Act
		var cut = RenderForUser(principal);

		// Assert — the roles card Author span must carry green Tailwind classes
		var authorBadge = cut.FindAll("span")
				.FirstOrDefault(span =>
						span.TextContent.Trim() == "Author"
						&& span.GetAttribute("class") is { } cls
						&& cls.Contains("bg-green-100"));

		authorBadge.Should().NotBeNull("Non-admin role should render with green-100 background");
		authorBadge!.GetAttribute("class").Should().Contain("text-green-800");
	}

	[Fact]
	public void Profile_AdminHeaderBadge_HasRedBackgroundClass()
	{
		// Arrange
		var principal = CreatePrincipal(
				name: "Super Admin",
				email: "superadmin@example.com",
				userId: "auth0|super",
				pictureUrl: null,
				rolesJson: null,
				extraClaims: [new Claim(ClaimTypes.Role, "Admin")]);

		// Act
		var cut = RenderForUser(principal);

		// Assert — the header-area Admin badge (title="Administrator") must use red-600 bg
		var headerBadge = cut.FindAll("span")
				.FirstOrDefault(span =>
						span.GetAttribute("title") == "Administrator"
						&& span.GetAttribute("class") is { } cls
						&& cls.Contains("bg-red-600"));

		headerBadge.Should().NotBeNull("Header Admin badge should render with bg-red-600");
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
