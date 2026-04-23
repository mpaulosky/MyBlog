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
			await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

			var brandLink = page.Locator("header a[href=\"/\"]");
			await brandLink.WaitForAsync(new() { Timeout = 5000 });

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
			await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

			// Scope to header to avoid matching the home-page CTA login button
			var loginLink = page.Locator("header a[href*=\"/account/login\"]").First;
			await loginLink.WaitForAsync(new() { Timeout = 5000 });

			// Assert
			var isVisible = await loginLink.IsVisibleAsync();
			isVisible.Should().BeTrue();
		});
	}

	[Fact]
	public async Task Layout_NavMenu_IsHiddenWhenNotAuthenticated()
	{
		// Arrange

		await InteractWithPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/");
			await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

			// The <nav> element is always present but its links are wrapped in <AuthorizeView>.
			// For anonymous users the nav is empty — no NavLink anchors inside it.
			// Wait for the layout to be interactive before checking nav state
			var nav = page.Locator("nav[aria-label=\"Main navigation\"]");
			await nav.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 5000 });

			var navLinkCount = await page.Locator("nav[aria-label=\"Main navigation\"] a").CountAsync();

			// Assert
			navLinkCount.Should().Be(0);
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
			await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

			var footer = page.Locator("footer[role=\"contentinfo\"]");
			await footer.WaitForAsync(new() { Timeout = 5000 });

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
			await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

			// The brightness toggle button is always rendered in the header
			var toggleBtn = page.Locator("button[aria-label=\"Toggle brightness\"]");
			await toggleBtn.WaitForAsync(new() { Timeout = 5000 });

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
			await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

			// The color-picker button is always rendered in the header
			var schemeBtn = page.Locator("button[aria-label=\"Choose color theme\"]");
			await schemeBtn.WaitForAsync(new() { Timeout = 5000 });

			// Assert
			var isVisible = await schemeBtn.IsVisibleAsync();
			isVisible.Should().BeTrue();
		});
	}
}
