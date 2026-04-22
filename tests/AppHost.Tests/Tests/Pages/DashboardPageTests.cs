// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     DashboardPageTests.cs
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
/// Playwright E2E tests for the Dashboard page (requires authentication).
/// Tests are skipped automatically when Auth0 credentials are not configured.
/// </summary>
public class DashboardPageTests : BasePlaywrightTests
{
	public DashboardPageTests(AspireManager aspireManager) : base(aspireManager) { }

	[Fact]
	public async Task DashboardPage_LoadsWithoutRedirect()
	{
		// Arrange / Act / Assert
		await InteractWithAuthenticatedPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/dashboard");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Assert
			page.Url.Should().Contain("/dashboard");
		});
	}

	[Fact]
	public async Task DashboardPage_ShowsWelcomeHeading()
	{
		// Arrange / Act / Assert
		await InteractWithAuthenticatedPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/dashboard");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var heading = page.Locator("h2").Filter(new() { HasText = "Welcome back" });
			await heading.WaitForAsync();

			// Assert — welcome section uses h2 (HeaderComponent renders the page-title h1)
			var text = await heading.TextContentAsync();
			text.Should().Contain("Welcome back");
		});
	}

	[Fact]
	public async Task DashboardPage_ShowsStatCards()
	{
		// Arrange / Act / Assert
		await InteractWithAuthenticatedPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/dashboard");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Assert — all four stat card labels must be present
			var totalIssues = page.Locator("dt:has-text('Total Issues')");
			var openIssues = page.Locator("dt:has-text('Open Issues')");
			var resolvedIssues = page.Locator("dt:has-text('Resolved Issues')");
			var thisWeek = page.Locator("dt:has-text('This Week')");

			await totalIssues.WaitForAsync();
			await openIssues.WaitForAsync();
			await resolvedIssues.WaitForAsync();
			await thisWeek.WaitForAsync();

			(await totalIssues.CountAsync()).Should().BeGreaterThan(0);
			(await openIssues.CountAsync()).Should().BeGreaterThan(0);
			(await resolvedIssues.CountAsync()).Should().BeGreaterThan(0);
			(await thisWeek.CountAsync()).Should().BeGreaterThan(0);
		});
	}
}
