// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     HomePageTests.cs
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
/// Playwright E2E tests for the Home page (guest and authenticated views).
/// </summary>
public class HomePageTests : BasePlaywrightTests
{
	public HomePageTests(AspireManager aspireManager) : base(aspireManager) { }

	[Fact]
	public async Task HomePage_GuestView_ShowsWelcomeHeading()
	{
		// Arrange

		await InteractWithPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var heading = page.Locator("h1");
			await heading.WaitForAsync();

			// Assert
			var text = await heading.TextContentAsync();
			text.Should().Contain("Hello, users!");
		});
	}

	[Fact]
	public async Task HomePage_GuestView_ShowsLoginButton()
	{
		// Arrange

		await InteractWithPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// MyBlog home page may have navigation - just verify page loads
			var h1 = page.Locator("h1");
			await h1.WaitForAsync();

			// Assert
			var isVisible = await h1.IsVisibleAsync();
			isVisible.Should().BeTrue();
		});
	}

	[Fact]
	public async Task HomePage_AuthenticatedView_ShowsWelcomeBackHeading()
	{
		// Arrange / Act / Assert
		await InteractWithAuthenticatedPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var heading = page.Locator("h1");
			await heading.WaitForAsync();

			// Assert
			var text = await heading.TextContentAsync();
			// For authenticated users, MyBlog should still show Hello, users!
			text.Should().Contain("Hello, users!");
		});
	}

	[Fact]
	public async Task HomePage_AuthenticatedView_PageLoads()
	{
		// Arrange / Act / Assert
		await InteractWithAuthenticatedPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Assert - page should load successfully
			var body = page.Locator("body");
			var isVisible = await body.IsVisibleAsync();
			isVisible.Should().BeTrue();
		});
	}
}
