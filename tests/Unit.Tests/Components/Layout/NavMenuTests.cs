// ============================================
// Copyright (c) 2025. All rights reserved.
// File Name :     NavMenuTests.cs
// Company :       mpaulosky
// Author :        mpaulosky
// Solution Name : MyBlog
// Project Name :  Unit.Tests
// =============================================
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

using MyBlog.Unit.Tests.Testing;
using MyBlog.Web.Components.Layout;

namespace MyBlog.Unit.Tests.Components.Layout;

public class NavMenuTests : BunitContext
{
	public NavMenuTests()
	{
		Services.AddAuthorizationCore();
		Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();
	}

	[Fact]
	public void UnauthenticatedUser_SeesLoginAndNoProtectedLinks()
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
	public void AuthenticatedAdmin_UsesDisplayNameAsProfileLabel_AndShowsAdminLinks()
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
	public void AuthenticatedUser_WithoutName_FallsBackToProfileLabel()
	{
		// Arrange (none)
		// Act
		var cut = RenderForUser(CreatePrincipal(roles: ["Author"]));

		cut.Markup.Should().Contain(">Profile<");
		cut.Markup.Should().Contain("New Post");
		cut.Markup.Should().NotContain("Manage Users");
	}

	[Fact]
	public void NavMenu_LoadsThemeFromJs_AndAllowsThemeInteraction()
	{
		// Arrange
		JSInterop.Mode = JSRuntimeMode.Loose;
		JSInterop.Setup<string>("themeManager.getColor").SetResult("green");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("dark");

		// Act
		var cut = RenderForUser(CreatePrincipal(name: "Theme User", roles: ["Admin"]));

		// Assert
		cut.WaitForAssertion(() => cut.Markup.Should().Contain("Theme User"));
		cut.Find("select").Change("yellow");
		cut.FindAll("button").Last().Click();

		JSInterop.Invocations.Should().Contain(invocation => invocation.Identifier == "themeManager.getColor");
		JSInterop.Invocations.Should().Contain(invocation => invocation.Identifier == "themeManager.getBrightness");
		JSInterop.Invocations.Should().Contain(invocation => invocation.Identifier == "themeManager.setColor");
		JSInterop.Invocations.Should().Contain(invocation => invocation.Identifier == "themeManager.setBrightness");
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
