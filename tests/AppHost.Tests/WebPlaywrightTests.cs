// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     WebPlaywrightTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : MyBlog
// Project Name :  AppHost.Tests
// =============================================

using AppHost.Infrastructure;

using FluentAssertions;

namespace AppHost;

/// <summary>
/// Playwright tests for the MyBlog web resource.
/// </summary>
public class WebPlaywrightTests(AspireManager aspireManager) : BasePlaywrightTests(aspireManager)
{
	[SkipInCIFact]
	public async Task WebHomePageLoads()
	{
		await InteractWithPageAsync("web", async page =>
		{
			await page.GotoAsync("/");

			var title = await page.TitleAsync();
			title.Should().NotBeNullOrEmpty();
		});
	}

	[SkipInCIFact]
	public async Task WebHomePageHasContent()
	{

		await InteractWithPageAsync("web", async page =>
		{
			await page.GotoAsync("/");

			var heading = page.Locator("h1, h2");
			await heading.WaitForAsync();
			var headingText = await heading.TextContentAsync();

			headingText.Should().NotBeNullOrEmpty();
		});
	}
}
