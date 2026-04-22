// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     IssueIndexPageTests.cs
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
/// Playwright E2E tests for the Issues index page (requires authentication).
/// Tests are skipped automatically when Auth0 credentials are not configured.
/// </summary>
public class IssueIndexPageTests : BasePlaywrightTests
{
	public IssueIndexPageTests(AspireManager aspireManager) : base(aspireManager) { }

	[Fact]
	public async Task IssueIndexPage_LoadsWithoutRedirect()
	{
		// Arrange / Act / Assert
		await InteractWithAuthenticatedPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/issues");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Assert
			page.Url.Should().Contain("/issues");
		});
	}

	[Fact]
	public async Task IssueIndexPage_HasIssuesPageTitle()
	{
		// Arrange / Act / Assert
		await InteractWithAuthenticatedPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/issues");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Assert
			var title = await page.TitleAsync();
			title.Should().Contain("IssueTracker");
		});
	}
}
