//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     NavMenuTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests.Bunit
//=======================================================

using Microsoft.Extensions.DependencyInjection;

using MyBlog.Web.Components.Layout;
using MyBlog.Web.Components.Theme;

using Web.Testing;

namespace Web.Components.Layout;

public class NavMenuTests : BunitContext
{
	public NavMenuTests()
	{
		Services.AddAuthorizationCore();
		Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();
	}

	[Fact]
	public void UnauthenticatedUserSeesLoginAndNoProtectedLinks()
	{
		// Arrange (none)
		// Act
		var cut = RenderForUser(CreatePrincipal(authenticated: false));

		cut.Markup.Should().Contain("Login");
		cut.Markup.Should().NotContain("Logout");
		cut.Markup.Should().NotContain("Manage Users");
		cut.Markup.Should().NotContain("New Post");
	}

	[Fact]
	public void AuthenticatedAdminUsesDisplayNameAsProfileLabelAndShowsAdminLinks()
	{
		// Arrange (none)
		// Act
		var cut = RenderForUser(CreatePrincipal(name: "Admin User", roles: ["Admin"]));

		cut.Markup.Should().Contain("Admin User");
		cut.Markup.Should().Contain("Manage Users");
		cut.Markup.Should().Contain("New Post");
		cut.Markup.Should().Contain("Logout");
		cut.Markup.Should().NotContain("Logout (Admin User)");
	}

	[Fact]
	public void AuthenticatedUserWithoutNameFallsBackToProfileLabel()
	{
		// Arrange (none)
		// Act
		var cut = RenderForUser(CreatePrincipal(roles: ["Author"]));

		cut.Markup.Should().Contain(">Profile<");
		cut.Markup.Should().Contain("New Post");
		cut.Markup.Should().NotContain("Manage Users");
	}

	[Fact]
	public void NavMenuLoadsThemeFromJsAndAllowsThemeInteraction()
	{
		// Arrange
		JSInterop.Mode = JSRuntimeMode.Loose;
		JSInterop.Setup<string>("themeManager.getColor").SetResult("green");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("dark");
		JSInterop.SetupVoid("themeManager.setColor", "yellow");
		JSInterop.SetupVoid("themeManager.setBrightness", "light");

		var principal = CreatePrincipal(name: "Theme User", roles: ["Admin"]);

		// Act — render ThemeProvider wrapping NavMenu so cascading values flow through
		var cut = Render<ThemeProvider>(parameters => parameters
				.AddCascadingValue(Task.FromResult(new AuthenticationState(principal)))
				.AddChildContent<NavMenu>());

		// Assert — wait for JS theme loading and username to appear
		cut.WaitForAssertion(() =>
		{
			cut.Markup.Should().Contain("Theme User");
			JSInterop.Invocations.Should().Contain(inv => inv.Identifier == "themeManager.getColor");
			JSInterop.Invocations.Should().Contain(inv => inv.Identifier == "themeManager.getBrightness");
		});

		// Interact with theme controls
		cut.Find("select").Change("yellow");
		cut.FindAll("button").Last().Click();

		// Assert JS set-calls were triggered
		cut.WaitForAssertion(() =>
		{
			JSInterop.Invocations.Should().Contain(inv => inv.Identifier == "themeManager.setColor");
			JSInterop.Invocations.Should().Contain(inv => inv.Identifier == "themeManager.setBrightness");
		});
	}

	[Fact]
	public void NavMenuRendersInsideHeaderElement()
	{
		// Arrange (none)
		// Act
		var cut = RenderForUser(CreatePrincipal(authenticated: false));

		// Assert — NavMenu must be wrapped in a <header> landmark element
		cut.Find("header").Should().NotBeNull();
		cut.Find("header nav").Should().NotBeNull();
	}

	[Fact]
	public void NavMenuBrandNavLinkPointsToRoot()
	{
		// Arrange (none)
		// Act
		var cut = RenderForUser(CreatePrincipal(authenticated: false));

		// Assert — brand link navigates to "/"
		var brandLink = cut.Find("a[href='/']");
		brandLink.TextContent.Should().Contain("MyBlog");
	}

	private IRenderedComponent<NavMenu> RenderForUser(ClaimsPrincipal principal)
	{
		return Render<NavMenu>(parameters => parameters
				.AddCascadingValue(Task.FromResult(new AuthenticationState(principal))));
	}

	private static ClaimsPrincipal CreatePrincipal(bool authenticated = true, string? name = null, string[]? roles = null)
	{
		if (!authenticated)
		{
			return new ClaimsPrincipal(new ClaimsIdentity());
		}

		var claims = new List<Claim>();

		if (!string.IsNullOrWhiteSpace(name))
		{
			claims.Add(new Claim(ClaimTypes.Name, name));
		}

		if (roles is not null)
		{
			claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
		}

		return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, ClaimTypes.Role));
	}
}
