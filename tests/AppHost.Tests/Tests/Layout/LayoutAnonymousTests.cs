// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     LayoutAnonymousTests.cs
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
/// Playwright E2E tests for the Web application layout visible to anonymous (unauthenticated) users.
/// </summary>
public class LayoutAnonymousTests : BasePlaywrightTests
{
	public LayoutAnonymousTests(AspireManager aspireManager) : base(aspireManager) { }

	[Fact]
	public async Task Layout_Header_ShowsBrandLink()
	{
		// Arrange

		await InteractWithPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// font-bold targets the brand link only (mobile Home link lacks this class)
			var brandLink = page.Locator("header a[href=\"/\"][class*=\"font-bold\"]");
			await brandLink.WaitForAsync();

			// Assert
			var text = await brandLink.TextContentAsync();
			text.Should().NotBeNullOrEmpty();
		});
	}

	[Fact]
	public async Task Layout_Header_ShowsLoginLinkWhenNotAuthenticated()
	{
		// Arrange

		await InteractWithPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Scope to header to avoid matching the home-page CTA login button
			var loginLink = page.Locator("header a[href*=\"/account/login\"]").First;
			await loginLink.WaitForAsync();

			// Assert
			var isVisible = await loginLink.IsVisibleAsync();
			isVisible.Should().BeTrue();
		});
	}

	[Fact]
	public async Task Layout_NavMenu_AuthLinksAreHiddenWhenNotAuthenticated()
	{
		// Arrange

		await InteractWithPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// The <nav> element is always present. For anonymous users, only public links are shown.
			// "Blog Posts" link is always visible (not auth-protected), but profile, new post, etc. are hidden.
			var nav = page.Locator("nav[aria-label=\"Main navigation\"]");
			await nav.WaitForAsync();

			// Check that auth-protected links are NOT visible
			var profileLink = page.Locator("nav[aria-label=\"Main navigation\"] a[href=\"profile\"]");
			var profileCount = await profileLink.CountAsync();

			// Assert
			profileCount.Should().Be(0, "profile link should not be visible to anonymous users");
		});
	}

	[Fact]
	public async Task Layout_Footer_ShowsCopyrightText()
	{
		// Arrange

		await InteractWithPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			var footer = page.Locator("footer[role=\"contentinfo\"]");
			await footer.WaitForAsync();

			// Assert
			var text = await footer.TextContentAsync();
			text.Should().NotBeNullOrEmpty();
		});
	}

	[Fact]
	public async Task Layout_ThemeToggleButton_IsVisible()
	{
		// Arrange

		await InteractWithPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// The brightness toggle button is rendered in header; .First targets desktop (mobile duplicate hidden by default)
			var toggleBtn = page.Locator("button[aria-label*=\"Toggle dark mode\"]").First;
			await toggleBtn.WaitForAsync();

			// Assert
			var isVisible = await toggleBtn.IsVisibleAsync();
			isVisible.Should().BeTrue();
		});
	}

	[Fact]
	public async Task Layout_ColorSchemeButton_IsVisible()
	{
		// Arrange

		await InteractWithPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// The color-picker dropdown is always rendered in the header; .First targets desktop (mobile duplicate hidden by default)
			var schemeBtn = page.Locator("select[aria-label=\"Choose color theme\"]").First;
			await schemeBtn.WaitForAsync();

			// Assert
			var isVisible = await schemeBtn.IsVisibleAsync();
			isVisible.Should().BeTrue();
		});
	}
}
