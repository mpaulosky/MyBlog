//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     ThemeProvider.razor.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MyBlog.Web.Components.Theme;

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

		CurrentColor = await GetStoredThemeValueAsync("themeManager.getColor", CurrentColor);
		CurrentBrightness = await GetStoredThemeValueAsync("themeManager.getBrightness", CurrentBrightness);

		await TryMarkInitializedAsync();
		await InvokeAsync(StateHasChanged);
	}

	public async Task SetColor(string color)
	{
		CurrentColor = color;
		StateHasChanged();
		await Js.InvokeVoidAsync("themeManager.setColor", color);
	}

	public async Task SetBrightness(string brightness)
	{
		CurrentBrightness = brightness;
		StateHasChanged();
		await Js.InvokeVoidAsync("themeManager.setBrightness", brightness);
	}

	private async Task<string> GetStoredThemeValueAsync(string identifier, string fallback)
	{
		try
		{
			return await Js.InvokeAsync<string>(identifier);
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
			await Js.InvokeVoidAsync("themeManager.markInitialized");
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
