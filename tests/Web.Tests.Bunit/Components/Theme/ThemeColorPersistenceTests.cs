//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     ThemeColorPersistenceTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests.Bunit
//=======================================================

using Microsoft.JSInterop;

using MyBlog.Web.Components.Theme;

namespace Web.Components.Theme;

/// <summary>
/// Focused tests for issue #239: colour selection persists across navigation and
/// reload, all four Tailwind palette colours are supported end-to-end, and layout
/// surfaces receive the persisted colour via cascading values.
/// </summary>
public sealed class ThemeColorPersistenceTests : BunitContext
{
	public ThemeColorPersistenceTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
	}

	// ─── Persistence: reading back the stored value on a fresh mount ──────────

	[Theory]
	[InlineData("red")]
	[InlineData("blue")]
	[InlineData("green")]
	[InlineData("yellow")]
	public void ThemeProviderAdoptsAllFourStoredColors_OnFirstRender(string storedColor)
	{
		// Arrange — localStorage (via themeManager.getColor) holds the previously stored colour
		JSInterop.Setup<string>("themeManager.getColor").SetResult(storedColor);
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");

		// Act — new ThemeProvider instance mounts (simulates what happens after page navigation)
		var cut = Render<ThemeProvider>();

		// Assert — component adopts the persisted colour so the page reflects the stored palette
		cut.WaitForAssertion(() =>
			cut.Instance.CurrentColor.Should().Be(storedColor,
				because: $"ThemeProvider must read '{storedColor}' back from JS/localStorage on mount"));
	}

	[Theory]
	[InlineData("light")]
	[InlineData("dark")]
	public void ThemeProviderAdoptsBothStoredBrightnessValues_OnFirstRender(string storedBrightness)
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getColor").SetResult("blue");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult(storedBrightness);

		// Act
		var cut = Render<ThemeProvider>();

		// Assert
		cut.WaitForAssertion(() =>
			cut.Instance.CurrentBrightness.Should().Be(storedBrightness,
				because: $"ThemeProvider must read '{storedBrightness}' back from JS/localStorage on mount"));
	}

	// ─── Persistence: writing to storage on colour selection ─────────────────

	[Theory]
	[InlineData("red")]
	[InlineData("blue")]
	[InlineData("green")]
	[InlineData("yellow")]
	public async Task ThemeProviderSetColor_WritesToJs_ForAllSupportedPaletteColors(string newColor)
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getColor").SetResult("blue");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");

		var cut = Render<ThemeProvider>();

		// Act — await so the full async write path (InvokeVoidAsync) completes before asserting
		await cut.InvokeAsync(() => cut.Instance.SetColor(newColor));

		// Assert — JS setColor is invoked, which writes to localStorage (read back on next page load)
		JSInterop.Invocations.Should().Contain(i =>
			i.Identifier == "themeManager.setColor" &&
			i.Arguments.Contains(newColor),
			because: $"SetColor('{newColor}') must persist to localStorage via themeManager.setColor");
	}

	[Theory]
	[InlineData("light")]
	[InlineData("dark")]
	public async Task ThemeProviderSetBrightness_WritesToJs_ForBothSupportedValues(string newBrightness)
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getColor").SetResult("blue");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");

		var cut = Render<ThemeProvider>();

		// Act — await so the full async write path completes before asserting
		await cut.InvokeAsync(() => cut.Instance.SetBrightness(newBrightness));

		// Assert
		JSInterop.Invocations.Should().Contain(i =>
			i.Identifier == "themeManager.setBrightness" &&
			i.Arguments.Contains(newBrightness),
			because: $"SetBrightness('{newBrightness}') must persist to localStorage via themeManager.setBrightness");
	}

	// ─── Navigation simulation: new ThemeProvider instance reads stored value ─

	[Fact]
	public void ThemeProvider_SimulatedNavigation_ReadsStoredColorFromJs()
	{
		// Arrange — simulate: user previously selected "green" and it was written to localStorage
		JSInterop.Setup<string>("themeManager.getColor").SetResult("green");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");

		// Act — simulate page navigation: a new ThemeProvider instance mounts on the destination page
		var cut = Render<ThemeProvider>(parameters => parameters
			.AddChildContent("<span id='destination'>Destination Page</span>"));

		// Assert — the new page reflects the stored colour from localStorage
		cut.WaitForAssertion(() =>
		{
			cut.Instance.CurrentColor.Should().Be("green",
				because: "after navigation the new page must display the previously persisted colour");
			cut.Find("#destination").TextContent.Should().Be("Destination Page");
		});
	}

	[Fact]
	public void ThemeProvider_SimulatedNavigation_ReadsStoredDarkBrightnessFromJs()
	{
		// Arrange — simulate: user previously enabled dark mode
		JSInterop.Setup<string>("themeManager.getColor").SetResult("blue");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("dark");

		// Act — simulate navigation
		var cut = Render<ThemeProvider>();

		// Assert
		cut.WaitForAssertion(() =>
			cut.Instance.CurrentBrightness.Should().Be("dark",
				because: "brightness preference must survive navigation via localStorage"));
	}

	// ─── Cascade: layout surfaces receive the persisted colour ────────────────

	[Fact]
	public void ThemeProvider_CascadesStoredColor_ToColorDropdown()
	{
		// Arrange — stored colour is "yellow"
		JSInterop.Setup<string>("themeManager.getColor").SetResult("yellow");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");

		// Act — ThemeProvider wraps ThemeSelector (same as production: App → ThemeProvider → NavMenu → ThemeSelector)
		var cut = Render<ThemeProvider>(parameters => parameters
			.AddChildContent<ThemeSelector>());

		// Assert — the colour dropdown reflects "yellow" through the cascading value
		cut.WaitForAssertion(() =>
			cut.Find("select").GetAttribute("value").Should().Be("yellow",
				because: "the colour dropdown must display the persisted colour after a new page mounts"));
	}

	[Fact]
	public void ThemeProvider_CascadesDarkBrightness_ToBrightnessToggleAriaLabel()
	{
		// Arrange — stored brightness is dark
		JSInterop.Setup<string>("themeManager.getColor").SetResult("blue");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("dark");

		// Act
		var cut = Render<ThemeProvider>(parameters => parameters
			.AddChildContent<ThemeSelector>());

		// Assert — brightness toggle reflects dark state through the cascaded value
		cut.WaitForAssertion(() =>
			cut.Find("button[aria-label]").GetAttribute("aria-label").Should().Contain("dark",
				because: "the brightness toggle must reflect the persisted dark mode after navigation"));
	}

	// ─── SetColor updates the internal state AND cascades ─────────────────────

	[Fact]
	public async Task ThemeProvider_AfterSetColorRed_CurrentColorIsRed_AndColorDropdownReflectsIt()
	{
		// Arrange — starts with blue
		JSInterop.Setup<string>("themeManager.getColor").SetResult("blue");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");

		var cut = Render<ThemeProvider>(parameters => parameters
			.AddChildContent<ThemeSelector>());

		// Act — user picks red; await the full async write path
		await cut.InvokeAsync(() => cut.Instance.SetColor("red"));

		// Assert — internal state updated AND cascaded to child dropdown
		cut.WaitForAssertion(() =>
		{
			cut.Instance.CurrentColor.Should().Be("red");
			cut.Find("select").GetAttribute("value").Should().Be("red",
				because: "the colour dropdown must immediately reflect the newly selected colour");
		});
	}

	// ─── Guard: getColor / getBrightness called exactly once per mount ────────

	[Fact]
	public void ThemeProvider_GetColor_IsInvokedExactlyOnce_PerMount()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getColor").SetResult("blue");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");

		// Act
		var cut = Render<ThemeProvider>();

		// Wait for the first-render async path to complete
		cut.WaitForAssertion(() =>
			JSInterop.Invocations.Should().Contain(i => i.Identifier == "themeManager.getColor"));

		// Assert — exactly one call; the firstRender guard prevents repeated localStorage reads
		JSInterop.Invocations.Count(i => i.Identifier == "themeManager.getColor")
			.Should().Be(1, because: "ThemeProvider must read colour from JS only on firstRender, not on every re-render");
	}

	[Fact]
	public void ThemeProvider_GetBrightness_IsInvokedExactlyOnce_PerMount()
	{
		// Arrange
		JSInterop.Setup<string>("themeManager.getColor").SetResult("blue");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");

		// Act
		var cut = Render<ThemeProvider>();

		cut.WaitForAssertion(() =>
			JSInterop.Invocations.Should().Contain(i => i.Identifier == "themeManager.getBrightness"));

		// Assert
		JSInterop.Invocations.Count(i => i.Identifier == "themeManager.getBrightness")
			.Should().Be(1, because: "ThemeProvider must read brightness from JS only on firstRender");
	}

	// ─── JS-failure branch: catch block swallows the exception ──────────────

	[Fact]
	public async Task ThemeProvider_SetColor_SilentlySwallowsJsException_AndKeepsNewColor()
	{
		// Arrange — Strict mode: themeManager.setColor is not planned, so bUnit throws JSException;
		// the catch block in SetColor must absorb it without propagating to the caller.
		JSInterop.Mode = JSRuntimeMode.Strict;
		JSInterop.Setup<string>("themeManager.getColor").SetResult("blue");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");
		// themeManager.setColor deliberately NOT planned — exercises the catch branch

		var cut = Render<ThemeProvider>();
		cut.WaitForAssertion(() => cut.Instance.CurrentColor.Should().Be("blue"));

		// Act — must not throw even though JS persistence will fail
		await cut.InvokeAsync(() => cut.Instance.SetColor("red"));

		// Assert — CurrentColor updated to the new value despite the JS failure
		cut.Instance.CurrentColor.Should().Be("red",
			because: "SetColor must update CurrentColor even when the JS persistence call throws");
	}

	[Fact]
	public async Task ThemeProvider_SetBrightness_SilentlySwallowsJsException_AndKeepsNewBrightness()
	{
		// Arrange — Strict mode: themeManager.setBrightness is not planned
		JSInterop.Mode = JSRuntimeMode.Strict;
		JSInterop.Setup<string>("themeManager.getColor").SetResult("blue");
		JSInterop.Setup<string>("themeManager.getBrightness").SetResult("light");
		// themeManager.setBrightness deliberately NOT planned — exercises the catch branch

		var cut = Render<ThemeProvider>();
		cut.WaitForAssertion(() => cut.Instance.CurrentBrightness.Should().Be("light"));

		// Act — must not throw even though JS persistence will fail
		await cut.InvokeAsync(() => cut.Instance.SetBrightness("dark"));

		// Assert
		cut.Instance.CurrentBrightness.Should().Be("dark",
			because: "SetBrightness must update CurrentBrightness even when the JS persistence call throws");
	}
}
