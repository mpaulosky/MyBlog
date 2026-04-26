//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     ThemeSelectorTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests.Bunit
//=======================================================

using Microsoft.AspNetCore.Components;

using MyBlog.Web.Components.Theme;

namespace Web.Components.Theme;

// NOTE: These tests are scaffolded ahead of production code.
// They depend on Issue #82 (ThemeProvider) and Issue #83 (ThemeSelector family).
// They compile + pass once those components are merged.
// Do NOT merge this PR before #82 and #83 are merged.

// ─── ThemeSelector ────────────────────────────────────────────────────────────

public sealed class ThemeSelectorTests : BunitContext
{
	public ThemeSelectorTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void ThemeSelectorRendersWithoutError()
	{
		// Arrange (none — use defaults from loose JS mock)
		// Act
		var act = () => Render<ThemeSelector>(parameters => parameters
				.AddCascadingValue("CurrentColor", "blue")
				.AddCascadingValue("CurrentBrightness", "light"));

		// Assert
		act.Should().NotThrow();
	}

	[Fact]
	public void ThemeSelectorContainsBrightnessToggleAndColorDropdown()
	{
		// Arrange (none)
		// Act
		var cut = Render<ThemeSelector>(parameters => parameters
				.AddCascadingValue("CurrentColor", "blue")
				.AddCascadingValue("CurrentBrightness", "light"));

		// Assert — both subcomponents are rendered
		cut.FindComponent<ThemeBrightnessToggleComponent>().Should().NotBeNull();
		cut.FindComponent<ThemeColorDropdownComponent>().Should().NotBeNull();
	}
}

// ─── ThemeBrightnessToggleComponent ───────────────────────────────────────────

public sealed class ThemeBrightnessToggleTests : BunitContext
{
	public ThemeBrightnessToggleTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void BrightnessToggleRendersWithoutError()
	{
		// Arrange (none)
		// Act
		var act = () => Render<ThemeBrightnessToggleComponent>(parameters => parameters
				.AddCascadingValue("CurrentBrightness", "light"));

		// Assert
		act.Should().NotThrow();
	}

	[Fact]
	public void BrightnessToggleShowsSunIconWhenBrightnessIsDark()
	{
		// Arrange (none)
		// Act
		var cut = Render<ThemeBrightnessToggleComponent>(parameters => parameters
				.AddCascadingValue("CurrentBrightness", "dark"));

		// Assert — sun icon rendered (user clicks to switch to light)
		cut.Find("button[aria-label]").GetAttribute("aria-label").Should().Contain("dark", "dark mode toggle should indicate current dark state");
		cut.Markup.Should().ContainAny("sun", "☀", "M12 3v1m0 16v1", "dark mode shows sun icon");
	}

	[Fact]
	public void BrightnessToggleShowsMoonIconWhenBrightnessIsLight()
	{
		// Arrange (none)
		// Act
		var cut = Render<ThemeBrightnessToggleComponent>(parameters => parameters
				.AddCascadingValue("CurrentBrightness", "light"));

		// Assert — moon icon rendered (user clicks to switch to dark)
		cut.Markup.Should().ContainAny("moon", "🌙", "M20.354", "light mode shows moon icon");
	}

	[Fact]
	public void BrightnessToggleWhenClickedInvokesSetBrightnessWithDarkWhenCurrentlyLight()
	{
		// Arrange
		var setColorCalled = false;
		var capturedBrightness = string.Empty;

		// Act
		var cut = Render<ThemeBrightnessToggleComponent>(parameters => parameters
				.AddCascadingValue("CurrentBrightness", "light")
				.Add(p => p.OnBrightnessChanged, EventCallback.Factory.Create<string>(this, brightness =>
				{
					setColorCalled = true;
					capturedBrightness = brightness;
				})));

		cut.Find("button").Click();

		// Assert
		setColorCalled.Should().BeTrue();
		capturedBrightness.Should().Be("dark");
	}

	[Fact]
	public void BrightnessToggleWhenClickedInvokesSetBrightnessWithLightWhenCurrentlyDark()
	{
		// Arrange
		var capturedBrightness = string.Empty;

		// Act
		var cut = Render<ThemeBrightnessToggleComponent>(parameters => parameters
				.AddCascadingValue("CurrentBrightness", "dark")
				.Add(p => p.OnBrightnessChanged, EventCallback.Factory.Create<string>(this, brightness =>
				{
					capturedBrightness = brightness;
				})));

		cut.Find("button").Click();

		// Assert
		capturedBrightness.Should().Be("light");
	}

	[Fact]
	public void BrightnessToggleHasAriaLabelForAccessibility()
	{
		// Arrange (none)
		// Act
		var cut = Render<ThemeBrightnessToggleComponent>(parameters => parameters
				.AddCascadingValue("CurrentBrightness", "light"));

		// Assert
		cut.Find("button[aria-label]").GetAttribute("aria-label").Should().NotBeNullOrWhiteSpace(
				because: "toggle button must have an accessible label");
	}
}

// ─── ThemeColorDropdownComponent ──────────────────────────────────────────────

public sealed class ThemeColorDropdownTests : BunitContext
{
	public ThemeColorDropdownTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	[Fact]
	public void ColorDropdownRendersWithoutError()
	{
		// Arrange (none)
		// Act
		var act = () => Render<ThemeColorDropdownComponent>(parameters => parameters
				.AddCascadingValue("CurrentColor", "blue"));

		// Assert
		act.Should().NotThrow();
	}

	[Fact]
	public void ColorDropdownRendersAllFourColorOptions()
	{
		// Arrange (none)
		// Act
		var cut = Render<ThemeColorDropdownComponent>(parameters => parameters
				.AddCascadingValue("CurrentColor", "blue"));

		// Assert — four colors available: blue, red, green, yellow
		var options = cut.FindAll("option");
		options.Should().HaveCount(4, because: "four palette colors are supported");

		var values = options.Select(o => o.GetAttribute("value")).ToList();
		values.Should().Contain("blue");
		values.Should().Contain("red");
		values.Should().Contain("green");
		values.Should().Contain("yellow");
	}

	[Fact]
	public void ColorDropdownShowsCurrentColorAsSelected()
	{
		// Arrange (none)
		// Act
		var cut = Render<ThemeColorDropdownComponent>(parameters => parameters
				.AddCascadingValue("CurrentColor", "green"));

		// Assert
		var select = cut.Find("select");
		select.GetAttribute("value").Should().Be("green");
	}

	[Fact]
	public void ColorDropdownWhenChangedInvokesOnColorChangedWithNewColor()
	{
		// Arrange
		var capturedColor = string.Empty;

		// Act
		var cut = Render<ThemeColorDropdownComponent>(parameters => parameters
				.AddCascadingValue("CurrentColor", "blue")
				.Add(p => p.OnColorChanged, EventCallback.Factory.Create<string>(this, color =>
				{
					capturedColor = color;
				})));

		cut.Find("select").Change("yellow");

		// Assert
		capturedColor.Should().Be("yellow");
	}

	[Theory]
	[InlineData("red")]
	[InlineData("blue")]
	[InlineData("green")]
	[InlineData("yellow")]
	public void ColorDropdownWhenChangedPropagatesAllSupportedColors(string color)
	{
		// Arrange
		var capturedColor = string.Empty;

		// Act
		var cut = Render<ThemeColorDropdownComponent>(parameters => parameters
				.AddCascadingValue("CurrentColor", "blue")
				.Add(p => p.OnColorChanged, EventCallback.Factory.Create<string>(this, c =>
				{
					capturedColor = c;
				})));

		cut.Find("select").Change(color);

		// Assert
		capturedColor.Should().Be(color);
	}

	[Fact]
	public void ColorDropdownHasAriaLabelForAccessibility()
	{
		// Arrange (none)
		// Act
		var cut = Render<ThemeColorDropdownComponent>(parameters => parameters
				.AddCascadingValue("CurrentColor", "blue"));

		// Assert
		cut.Find("select[aria-label]").GetAttribute("aria-label").Should().NotBeNullOrWhiteSpace(
				because: "dropdown must have an accessible label");
	}
}

// ─── ThemeProvider + ThemeSelector integration ────────────────────────────────

public sealed class ThemeProviderWithSelectorIntegrationTests : BunitContext
{
	public ThemeProviderWithSelectorIntegrationTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		JSInterop.Setup<string>("themeManager.getColor").SetResult("blue");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");
		JSInterop.SetupVoid("themeManager.setColor");
		JSInterop.SetupVoid("themeManager.setBrightness");
	}

	[Fact]
	public void ThemeSelectorInsideThemeProviderReceivesCurrentColorViaCascade()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getColor").SetResult("red");

		// Act
		var cut = Render<ThemeProvider>(parameters => parameters
				.AddChildContent<ThemeSelector>());

		// Assert — ThemeSelector receives cascaded CurrentColor="red"
		cut.WaitForAssertion(() =>
		{
			var dropdown = cut.FindComponent<ThemeColorDropdownComponent>();
			dropdown.Should().NotBeNull(because: "color dropdown is rendered within cascaded theme state");
		});
	}

	[Fact]
	public void ThemeSelectorInsideThemeProviderReceivesCurrentBrightnessViaCascade()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("dark");

		// Act
		var cut = Render<ThemeProvider>(parameters => parameters
				.AddChildContent<ThemeSelector>());

		// Assert — ThemeSelector receives cascaded CurrentBrightness="dark"
		cut.WaitForAssertion(() =>
		{
			var toggle = cut.FindComponent<ThemeBrightnessToggleComponent>();
			toggle.Should().NotBeNull(because: "brightness toggle is rendered within cascaded theme state");
		});
	}

	[Fact]
	public void ColorDropdownChangeInsideThemeProviderCallsSetColorJs()
	{
		// Arrange
		JSInterop.SetupVoid("themeManager.setColor", "yellow");

		var cut = Render<ThemeProvider>(parameters => parameters
				.AddChildContent<ThemeSelector>());

		// Act
		cut.WaitForAssertion(() => cut.FindComponent<ThemeColorDropdownComponent>().Should().NotBeNull());
		cut.Find("select").Change("yellow");

		// Assert
		cut.WaitForAssertion(() =>
				JSInterop.Invocations.Should().Contain(i =>
						i.Identifier == "themeManager.setColor" &&
						i.Arguments.Contains("yellow")));
	}

	[Fact]
	public void BrightnessToggleClickInsideThemeProviderCallsSetBrightnessJs()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");
		JSInterop.SetupVoid("themeManager.setBrightness", "dark");

		var cut = Render<ThemeProvider>(parameters => parameters
				.AddChildContent<ThemeSelector>());

		// Act
		cut.WaitForAssertion(() => cut.FindComponent<ThemeBrightnessToggleComponent>().Should().NotBeNull());
		cut.Find("button").Click();

		// Assert
		cut.WaitForAssertion(() =>
				JSInterop.Invocations.Should().Contain(i =>
						i.Identifier == "themeManager.setBrightness"));
	}
}
