// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     WebPlaywrightTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  AppHost.Tests
// =============================================

using AppHost.Tests.Infrastructure;

using FluentAssertions;

namespace AppHost.Tests;

/// <summary>
/// Playwright tests for the IssueTrackerApp web resource.
/// </summary>
public class WebPlaywrightTests(AspireManager aspireManager) : BasePlaywrightTests(aspireManager)
{
	[Fact]
	public async Task WebHomePageLoads()
	{

		await InteractWithPageAsync("web", async page =>
		{
			await page.GotoAsync("/");

			var title = await page.TitleAsync();
			title.Should().NotBeNullOrEmpty();
		});
	}

	[Fact]
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
