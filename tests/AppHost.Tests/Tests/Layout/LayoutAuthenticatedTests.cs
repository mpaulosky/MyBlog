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

			// Assert — verify expected links are present
			// Home link (site logo) is in the header, outside the nav element
			var homeLink = page.Locator("a[href=\"/\"]");
			var dashLink = nav.Locator("a[href=\"/dashboard\"]");
			var issuesLink = nav.Locator("a[href=\"/issues\"]");
			var createLink = nav.Locator("a[href=\"/issues/create\"]");

			// AuthorizeView renders <Authorized> content asynchronously after auth state resolves.
			// Wait for the first nav link (inside AuthorizeView) to appear before asserting counts.
			await dashLink.First.WaitForAsync();
			(await homeLink.CountAsync()).Should().BeGreaterThan(0);
			(await dashLink.CountAsync()).Should().BeGreaterThan(0);
			(await issuesLink.CountAsync()).Should().BeGreaterThan(0);
			(await createLink.CountAsync()).Should().BeGreaterThan(0);
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
