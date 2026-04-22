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
			text.Should().Contain("Welcome to IssueTracker");
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

			var loginBtn = page.Locator("a:has-text(\"Log in to Get Started\"), button:has-text(\"Log in to Get Started\")");
			await loginBtn.WaitForAsync();

			// Assert
			var isVisible = await loginBtn.IsVisibleAsync();
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
			text.Should().Contain("Welcome back");
		});
	}

	[Fact]
	public async Task HomePage_AuthenticatedView_ShowsDashboardLink()
	{
		// Arrange / Act / Assert
		await InteractWithAuthenticatedPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var dashLink = page.Locator("a[href=\"/dashboard\"]:has-text(\"Go to Dashboard\")");
			await dashLink.WaitForAsync();

			// Assert
			var isVisible = await dashLink.IsVisibleAsync();
			isVisible.Should().BeTrue();
		});
	}
}
