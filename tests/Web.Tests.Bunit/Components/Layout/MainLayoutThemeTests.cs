//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     MainLayoutThemeTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests.Bunit
//=======================================================

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using MyBlog.Web.Components.Layout;
using MyBlog.Web.Components.Theme;

using Web.Testing;

namespace Web.Components.Layout;

/// <summary>
/// Tests that shared layout surfaces (NavMenu, MainLayout) honour the selected
/// primary colour palette — issue #239: "key common UI like nav menu and footer
/// should reflect those colours consistently."
/// </summary>
public sealed class MainLayoutThemeTests : BunitContext
{
	public MainLayoutThemeTests()
	{
		Services.AddAuthorizationCore();
		Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	// ─── NavMenu layout surface ───────────────────────────────────────────────

	[Fact]
	public void NavMenu_NavElement_HasPrimaryColorBackgroundClass()
	{
		// Arrange (none)
		// Act
		var cut = Render<NavMenu>(parameters => parameters
			.AddCascadingValue(Task.FromResult(new AuthenticationState(CreatePrincipal()))));

		// Assert — NavMenu must use bg-primary-* classes so the active palette is reflected across pages
		cut.Find("nav").OuterHtml.Should().Contain("bg-primary-600",
			because: "the NavMenu nav element must use bg-primary-600 to honour the selected colour theme");
	}

	[Fact]
	public void NavMenu_NavElement_HasPrimaryBorderClass()
	{
		// Arrange (none)
		// Act
		var cut = Render<NavMenu>(parameters => parameters
			.AddCascadingValue(Task.FromResult(new AuthenticationState(CreatePrincipal()))));

		// Assert
		cut.Find("nav").OuterHtml.Should().Contain("border-primary-",
			because: "NavMenu must use primary-colour borders to stay consistent with the selected palette");
	}

	[Fact]
	public void NavMenu_RenderedInsideThemeProvider_DropdownShowsStoredColor()
	{
		// Arrange — ThemeProvider holds the stored colour "red"
		JSInterop.Setup<string>("themeManager.getColor").SetResult("red");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");

		var principal = CreatePrincipal();

		// Act — ThemeProvider wraps NavMenu (matches production: App.razor → ThemeProvider → NavMenu)
		var cut = Render<ThemeProvider>(parameters => parameters
			.AddCascadingValue(Task.FromResult(new AuthenticationState(principal)))
			.AddChildContent<NavMenu>());

		// Assert — NavMenu's ThemeSelector dropdown reflects the stored colour via cascade
		cut.WaitForAssertion(() =>
			cut.Find("select").GetAttribute("value").Should().Be("red",
				because: "NavMenu's ThemeSelector must display the stored colour after navigation via cascade"));
	}

	[Fact]
	public void NavMenu_RenderedInsideThemeProvider_BrightnessToggleReflectsStoredDarkMode()
	{
		// Arrange — stored brightness is dark
		JSInterop.Setup<string>("themeManager.getColor").SetResult("blue");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("dark");

		var principal = CreatePrincipal();

		// Act
		var cut = Render<ThemeProvider>(parameters => parameters
			.AddCascadingValue(Task.FromResult(new AuthenticationState(principal)))
			.AddChildContent<NavMenu>());

		// Assert — brightness toggle aria-label reflects the dark state cascaded from ThemeProvider
		cut.WaitForAssertion(() =>
			cut.Find("button[aria-label]").GetAttribute("aria-label").Should().Contain("dark",
				because: "the brightness toggle must reflect persisted dark mode via the ThemeProvider cascade"));
	}

	// ─── MainLayout layout surface ────────────────────────────────────────────

	[Fact]
	public void MainLayout_RootDiv_HasPrimaryColorBackgroundClass()
	{
		// Arrange
		// Act
		var cut = Render<MainLayout>(parameters => parameters
			.AddCascadingValue(Task.FromResult(new AuthenticationState(CreatePrincipal("Layout User", ["Author"]))))
			.Add(p => p.Body, (RenderFragment)(builder => builder.AddContent(0, "page body"))));

		// Assert — root layout element must use primary-* colours so all pages reflect the active palette
		cut.Markup.Should().Contain("bg-primary-400",
			because: "MainLayout root element must use bg-primary-400 to reflect the selected colour theme");
	}

	[Fact]
	public void MainLayout_RootDiv_HasDarkModePrimaryBackgroundClass()
	{
		// Arrange
		// Act
		var cut = Render<MainLayout>(parameters => parameters
			.AddCascadingValue(Task.FromResult(new AuthenticationState(CreatePrincipal("Layout User", ["Author"]))))
			.Add(p => p.Body, (RenderFragment)(builder => builder.AddContent(0, string.Empty))));

		// Assert — dark mode variant must also use primary palette
		cut.Markup.Should().Contain("dark:bg-primary-800",
			because: "MainLayout must use dark:bg-primary-800 so dark mode also reflects the selected colour theme");
	}

	[Fact]
	public void MainLayout_RootDiv_HasPrimaryTextColorClass()
	{
		// Arrange
		// Act
		var cut = Render<MainLayout>(parameters => parameters
			.AddCascadingValue(Task.FromResult(new AuthenticationState(CreatePrincipal("Layout User", ["Author"]))))
			.Add(p => p.Body, (RenderFragment)(builder => builder.AddContent(0, string.Empty))));

		// Assert — text colour must also follow the primary palette
		cut.Markup.Should().Contain("text-primary-",
			because: "MainLayout text colour must use primary-palette classes for visual consistency");
	}

	// ─── Helpers ──────────────────────────────────────────────────────────────

	private static ClaimsPrincipal CreatePrincipal(string? name = null, string[]? roles = null)
	{
		if (name is null)
		{
			return new ClaimsPrincipal(new ClaimsIdentity());
		}

		var claims = new List<Claim> { new(ClaimTypes.Name, name) };

		if (roles is not null)
		{
			claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
		}

		return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, ClaimTypes.Role));
	}
}
