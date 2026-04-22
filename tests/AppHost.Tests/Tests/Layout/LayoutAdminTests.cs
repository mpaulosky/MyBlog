// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     LayoutAdminTests.cs
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
/// Playwright E2E tests for the Web application layout visible only to Admin-role users.
/// Authentication is performed via the Testing environment's <c>/test/login?role=admin</c>
/// cookie endpoint — no Auth0 or external credentials required.
/// </summary>
public class LayoutAdminTests : BasePlaywrightTests
{
	public LayoutAdminTests(AspireManager aspireManager) : base(aspireManager) { }

	[Fact]
	public async Task Layout_AdminNav_IsVisibleForAdminUser()
	{
		// Arrange / Act / Assert
		await InteractWithAdminPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Assert — admin link or admin nav section is present
			var adminLink = page.Locator("a[href^=\"/admin\"]").First;
			var isVisible = await adminLink.IsVisibleAsync();
			isVisible.Should().BeTrue("an Admin-role user should see the admin navigation link");
		});
	}

	[Fact]
	public async Task Layout_AdminNav_IsHiddenForNonAdminUser()
	{
		// Arrange / Act / Assert
		await InteractWithAuthenticatedPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Assert — admin link should NOT be present for a regular User-role account
			var adminLinkCount = await page.Locator("a[href^=\"/admin\"]").CountAsync();
			adminLinkCount.Should().Be(0, "a standard User-role account should not see admin navigation");
		});
	}
}
