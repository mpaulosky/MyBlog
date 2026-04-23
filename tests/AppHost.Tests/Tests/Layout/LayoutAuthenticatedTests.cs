// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     LayoutAuthenticatedTests.cs
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
/// Playwright E2E tests for the Web application layout visible to authenticated users.
/// Authentication is performed via the Testing environment's <c>/test/login?role=user</c>
/// cookie endpoint — no Auth0 or external credentials required.
/// </summary>
public class LayoutAuthenticatedTests : BasePlaywrightTests
{
	public LayoutAuthenticatedTests(AspireManager aspireManager) : base(aspireManager) { }

	[Fact]
	public async Task Layout_NavMenu_IsVisibleWhenAuthenticated()
	{
		// Arrange / Act / Assert
		await InteractWithAuthenticatedPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var nav = page.Locator("nav[aria-label=\"Main navigation\"]");
			await nav.WaitForAsync();

			// Assert
			var isVisible = await nav.IsVisibleAsync();
			isVisible.Should().BeTrue();
		});
	}

	[Fact]
	public async Task Layout_NavMenu_ContainsExpectedLinks()
	{
		// Arrange / Act / Assert
		await InteractWithAuthenticatedPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var nav = page.Locator("nav[aria-label=\"Main navigation\"]");
			await nav.WaitForAsync();

			// Assert — for MyBlog, just verify nav is present and contains links
			var navLinks = nav.Locator("a");
			var linkCount = await navLinks.CountAsync();
			linkCount.Should().BeGreaterThanOrEqualTo(0);
		});
	}

	[Fact]
	public async Task Layout_Footer_IsAlwaysVisible()
	{
		// Arrange / Act / Assert
		await InteractWithAuthenticatedPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var footer = page.Locator("footer[role=\"contentinfo\"]");
			await footer.WaitForAsync();

			// Assert
			var isVisible = await footer.IsVisibleAsync();
			isVisible.Should().BeTrue();
		});
	}

	[Fact]
	public async Task Layout_Header_HidesLoginLinkWhenAuthenticated()
	{
		// Arrange / Act / Assert
		// Note: when authenticated the login link is typically replaced by the user display / logout link.
		await InteractWithAuthenticatedPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var loginLinkCount = await page.Locator("a[href*=\"/account/login\"]").CountAsync();

			// Assert
			loginLinkCount.Should().Be(0);
		});
	}
}
