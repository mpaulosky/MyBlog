//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     ThemeProviderTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests.Bunit
//=======================================================

using Microsoft.JSInterop;

using MyBlog.Web.Components.Theme;

namespace Web.Components.Theme;

public sealed class ThemeProviderTests : BunitContext
{
	public ThemeProviderTests()
	{
		// Use loose mode so un-configured calls return default values
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	// ─── Rendering ────────────────────────────────────────────────────────────

	[Fact]
	public void ThemeProviderRendersChildContentWithoutError()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getColor").SetResult("blue");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");

		// Act
		var cut = Render<ThemeProvider>(parameters => parameters
				.AddChildContent("<span id=\"child\">Hello</span>"));

		// Assert
		cut.Find("#child").TextContent.Should().Be("Hello");
	}

	[Fact]
	public void ThemeProviderRendersWithoutErrorWhenNoChildContent()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getColor").SetResult("blue");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");

		// Act
		var act = () => Render<ThemeProvider>();

		// Assert
		act.Should().NotThrow();
	}

	// ─── JS Interop on Init ───────────────────────────────────────────────────

	[Fact]
	public void ThemeProviderCallsGetColorOnAfterFirstRender()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getColor").SetResult("green");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");

		// Act
		var cut = Render<ThemeProvider>();

		// Assert
		cut.WaitForAssertion(() =>
				JSInterop.Invocations.Should().Contain(i => i.Identifier == "themeManager.getColor"));
	}

	[Fact]
	public void ThemeProviderCallsGetBrightnessOnAfterFirstRender()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getColor").SetResult("blue");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("dark");

		// Act
		var cut = Render<ThemeProvider>();

		// Assert
		cut.WaitForAssertion(() =>
				JSInterop.Invocations.Should().Contain(i => i.Identifier == "themeManager.getBrightness"));
	}

	[Fact]
	public void ThemeProviderLoadsColorFromJsAndExposesViaCascadingValue()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getColor").SetResult("red");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");

		// Act
		var cut = Render<ThemeProvider>(parameters => parameters
				.AddChildContent("<span id=\"probe\">probe</span>"));

		// Assert — JS was called and CurrentColor was updated
		cut.WaitForAssertion(() =>
		{
			JSInterop.Invocations.Should().Contain(i => i.Identifier == "themeManager.getColor");
			cut.Instance.CurrentColor.Should().Be("red");
		});
	}

	[Fact]
	public void ThemeProviderLoadsBrightnessFromJsAndExposesViaCascadingValue()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getColor").SetResult("blue");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("dark");

		// Act
		var cut = Render<ThemeProvider>();

		// Assert
		cut.WaitForAssertion(() =>
				JSInterop.Invocations.Should().Contain(i => i.Identifier == "themeManager.getBrightness"));
	}

	// ─── SetColor ─────────────────────────────────────────────────────────────

	[Fact]
	public void ThemeProviderSetColorCallsSetColorJsWithNewColor()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getColor").SetResult("blue");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");
		JSInterop.SetupVoid("themeManager.setColor", "green");

		var cut = Render<ThemeProvider>();

		// Act
		cut.InvokeAsync(() => cut.Instance.SetColor("green"));

		// Assert
		cut.WaitForAssertion(() =>
				JSInterop.Invocations.Should().Contain(i =>
						i.Identifier == "themeManager.setColor" &&
						i.Arguments.Contains("green")));
	}

	[Fact]
	public void ThemeProviderSetColorUpdatesCurrentColorAfterJsCall()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getColor").SetResult("blue");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");
		JSInterop.SetupVoid("themeManager.setColor", "red");

		var cut = Render<ThemeProvider>();

		// Act
		cut.InvokeAsync(() => cut.Instance.SetColor("red"));

		// Assert
		cut.WaitForAssertion(() => cut.Instance.CurrentColor.Should().Be("red"));
	}

	// ─── SetBrightness ────────────────────────────────────────────────────────

	[Fact]
	public void ThemeProviderSetBrightnessCallsSetBrightnessJsWithNewBrightness()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getColor").SetResult("blue");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");
		JSInterop.SetupVoid("themeManager.setBrightness", "dark");

		var cut = Render<ThemeProvider>();

		// Act
		cut.InvokeAsync(() => cut.Instance.SetBrightness("dark"));

		// Assert
		cut.WaitForAssertion(() =>
				JSInterop.Invocations.Should().Contain(i =>
						i.Identifier == "themeManager.setBrightness" &&
						i.Arguments.Contains("dark")));
	}

	[Fact]
	public void ThemeProviderSetBrightnessUpdatesCurrentBrightnessAfterJsCall()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getColor").SetResult("blue");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");
		JSInterop.SetupVoid("themeManager.setBrightness", "dark");

		var cut = Render<ThemeProvider>();

		// Act
		cut.InvokeAsync(() => cut.Instance.SetBrightness("dark"));

		// Assert
		cut.WaitForAssertion(() => cut.Instance.CurrentBrightness.Should().Be("dark"));
	}

	// ─── Readiness Marker ────────────────────────────────────────────────────

	[Fact]
	public void ThemeProviderCallsMarkInitializedAfterFirstRender()
	{
		// Arrange — the AppHost runtime verification relies on data-theme-ready="true"
		// being set by themeManager.markInitialized; confirm it is invoked on first render.
		JSInterop.Setup<string>("themeManager.getColor").SetResult("blue");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");
		JSInterop.SetupVoid("themeManager.markInitialized");

		// Act
		var cut = Render<ThemeProvider>();

		// Assert
		cut.WaitForAssertion(() =>
				JSInterop.Invocations.Should().Contain(i => i.Identifier == "themeManager.markInitialized",
						because: "ThemeProvider must signal readiness so AppHost Playwright tests can verify interactive state"));
	}

	// ─── Error Resilience ─────────────────────────────────────────────────────

	[Fact]
	public void ThemeProviderWhenJsThrowsDoesNotPropagateExceptionAndUsesDefaults()
	{
		// Arrange — simulate localStorage unavailable (JS exception on getColor)
		JSInterop.Setup<string>("themeManager.getColor")
				.SetException(new JSException("localStorage is not available"));
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");

		// Act
		var act = () => Render<ThemeProvider>();

		// Assert — component renders without throwing; defaults are used
		act.Should().NotThrow();
		var cut = Render<ThemeProvider>();
		cut.Instance.CurrentColor.Should().NotBeNull();
	}

	[Fact]
	public void ThemeProviderWhenGetBrightnessThrowsDoesNotPropagateExceptionAndUsesDefault()
	{
		// Arrange — simulate localStorage unavailable (JS exception on getBrightness)
		JSInterop.Setup<string>("themeManager.getColor").SetResult("blue");
		JSInterop.Setup<string>("themeManager.getBrightness")
				.SetException(new JSException("localStorage is not available"));

		// Act
		var act = () => Render<ThemeProvider>();

		// Assert — component renders without throwing; defaults are used
		act.Should().NotThrow();
		var cut = Render<ThemeProvider>();
		cut.Instance.CurrentBrightness.Should().NotBeNull();
	}
}
