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
		if (!firstRender) return;

		try
		{
			CurrentColor = await Js.InvokeAsync<string>("themeManager.getColor");
		}
		catch
		{
			// Keep default if localStorage is unavailable
		}

		try
		{
			CurrentBrightness = await Js.InvokeAsync<string>("themeManager.getBrightness");
		}
		catch
		{
			// Keep default if localStorage is unavailable
		}

		StateHasChanged();
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
}
