//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     ThemeProvider.razor.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MyBlog.Web.Components.Theme;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Razor component discovery and bUnit rendering require the component type to remain public.")]
public partial class ThemeProvider : ComponentBase
{
	[Inject] private IJSRuntime Js { get; set; } = default!;

	[Parameter] public RenderFragment? ChildContent { get; set; }

	public string CurrentColor { get; private set; } = "blue";
	public string CurrentBrightness { get; private set; } = "light";

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
		{
			return;
		}

		var currentColor = await GetStoredThemeValueAsync("themeManager.getColor", CurrentColor).ConfigureAwait(false);
		var currentBrightness = await GetStoredThemeValueAsync("themeManager.getBrightness", CurrentBrightness).ConfigureAwait(false);

		await TryMarkInitializedAsync().ConfigureAwait(false);
		await InvokeAsync(() =>
		{
			CurrentColor = currentColor;
			CurrentBrightness = currentBrightness;
			StateHasChanged();
		}).ConfigureAwait(false);
	}

	public async Task SetColor(string color)
	{
		await Js.InvokeVoidAsync("themeManager.setColor", color).ConfigureAwait(false);
		await InvokeAsync(() =>
		{
			CurrentColor = color;
			StateHasChanged();
		}).ConfigureAwait(false);
	}

	public async Task SetBrightness(string brightness)
	{
		await Js.InvokeVoidAsync("themeManager.setBrightness", brightness).ConfigureAwait(false);
		await InvokeAsync(() =>
		{
			CurrentBrightness = brightness;
			StateHasChanged();
		}).ConfigureAwait(false);
	}

	private async Task<string> GetStoredThemeValueAsync(string identifier, string fallback)
	{
		try
		{
			return await Js.InvokeAsync<string>(identifier).ConfigureAwait(false);
		}
		catch (JSException)
		{
			// Keep the bootstrap-applied fallback when JS storage is unavailable.
			return fallback;
		}
		catch (JSDisconnectedException)
		{
			// Circuit teardown can race the initial theme read during shutdown.
			return fallback;
		}
	}

	private async Task TryMarkInitializedAsync()
	{
		try
		{
			await Js.InvokeVoidAsync("themeManager.markInitialized").ConfigureAwait(false);
		}
		catch (JSException)
		{
			// The provider still works without the optional readiness marker.
		}
		catch (JSDisconnectedException)
		{
			// Ignore disconnect races; they do not affect persisted theme state.
		}
	}
}
