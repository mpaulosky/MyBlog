// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AdminPageTests.cs
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
/// Playwright E2E tests for the Admin section of the Web application.
/// Authentication is performed via the Testing environment's <c>/test/login?role=admin</c>
/// cookie endpoint — no Auth0 or external credentials required.
/// </summary>
public class AdminPageTests : BasePlaywrightTests
{
	public AdminPageTests(AspireManager aspireManager) : base(aspireManager) { }

	[Fact]
	public async Task AdminDashboard_LoadsWithoutRedirect()
	{
		// Arrange / Act / Assert
		await InteractWithAdminPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/admin");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Assert — should stay on /admin, not redirect to login
			page.Url.Should().Contain("/admin");
		});
	}

	[Fact]
	public async Task AdminDashboard_ShowsAdminHeading()
	{
		// Arrange / Act / Assert
		await InteractWithAdminPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/admin");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Assert — HeaderComponent renders h1.text-2xl; filter by text to be specific
			var heading = page.Locator("h1").Filter(new() { HasText = "Admin Dashboard" });
			await heading.WaitForAsync();
			var text = await heading.InnerTextAsync();
			text.Should().Contain("Admin Dashboard");
		});
	}

	[Fact]
	public async Task AdminCategories_LoadsForAdminUser()
	{
		// Arrange / Act / Assert
		await InteractWithAdminPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/admin/categories");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Assert — accessible (not redirected to login or access-denied)
			page.Url.Should().Contain("/admin/categories");
		});
	}

	[Fact]
	public async Task AdminStatuses_LoadsForAdminUser()
	{
		// Arrange / Act / Assert
		await InteractWithAdminPageAsync("web", async page =>
		{
			// Act
			await page.GotoAsync("/admin/statuses");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Assert
			page.Url.Should().Contain("/admin/statuses");
		});
	}

	[Fact]
	public async Task AdminPage_RedirectsNonAdminUser()
	{
		// Arrange / Act / Assert
		await InteractWithAuthenticatedPageAsync("web", async page =>
		{
			// Act — regular User-role account navigates to /admin
			await page.GotoAsync("/admin");
			await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

			// Assert — non-admin should be redirected to the access-denied path
			page.Url.Should().Contain("/Account/AccessDenied",
				"a non-admin user should be redirected to the access-denied page");
		});
	}
}
