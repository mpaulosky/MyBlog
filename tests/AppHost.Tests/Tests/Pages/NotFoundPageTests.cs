// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     NotFoundPageTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  AppHost.Tests
// =============================================

using AppHost.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Playwright;

namespace AppHost.Tests;

/// <summary>
/// Playwright E2E tests for the Not Found (404) page.
/// </summary>
public class NotFoundPageTests : BasePlaywrightTests
{
	public NotFoundPageTests(AspireManager aspireManager) : base(aspireManager) { }

	[Fact]
	public async Task NotFoundPage_ShowsNotFoundHeading()
	{
		// Arrange

		await InteractWithPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/not-found");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var heading = page.Locator("h1, h2, h3").Filter(new LocatorFilterOptions
			{
				HasText = "Not Found"
			});
			await heading.WaitForAsync();

			// Assert
			var text = await heading.TextContentAsync();
			text.Should().Contain("Not Found");
		});
	}

	[Fact]
	public async Task NotFoundPage_ShowsHelpfulMessage()
	{
		// Arrange

		await InteractWithPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/not-found");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var message = page.Locator("text=Sorry, the content you are looking for does not exist");
			await message.WaitForAsync();

			// Assert
			var isVisible = await message.IsVisibleAsync();
			isVisible.Should().BeTrue();
		});
	}
}
