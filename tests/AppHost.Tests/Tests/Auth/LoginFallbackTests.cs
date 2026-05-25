//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     LoginFallbackTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  AppHost.Tests
//=======================================================

using AppHost.Tests.Infrastructure;

using FluentAssertions;

namespace AppHost.Tests;

[Collection(AppHostTestCollection.Name)]
public sealed class LoginFallbackTests(AspireManager aspireManager)
{
	[Fact]
	public async Task AccountLogin_InTestingWithPlaceholderAuth0Config_RedirectsToLocalTestLogin()
	{
		// Arrange
		var endpoint = aspireManager.App?.GetEndpoint("web", "https")
			?? throw new InvalidOperationException("The web endpoint was not available for AppHost tests.");

		using var handler = new HttpClientHandler
		{
			AllowAutoRedirect = false,
			ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
		};
		using var client = new HttpClient(handler) { BaseAddress = endpoint };

		// Act
		using var response = await client.GetAsync(
			new Uri("/Account/Login?returnUrl=/profile", UriKind.Relative),
			TestContext.Current.CancellationToken);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Redirect);
		response.Headers.Location.Should().NotBeNull();
		response.Headers.Location!.ToString().Should().StartWith("/test/login");
		response.Headers.Location!.ToString().Should().NotContain("test.auth0.com");
	}
}
