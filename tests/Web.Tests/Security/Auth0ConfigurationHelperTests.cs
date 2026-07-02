//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     Auth0ConfigurationHelperTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using MyBlog.Web.Security;

namespace Web.Security;

public sealed class Auth0ConfigurationHelperTests
{
	[Theory]
	[InlineData(null, "real-client-id")]
	[InlineData("", "real-client-id")]
	[InlineData("real-tenant.auth0.com", null)]
	[InlineData("real-tenant.auth0.com", "")]
	[InlineData("test.auth0.com", "real-client-id")]
	[InlineData("TEST.AUTH0.COM", "real-client-id")]
	[InlineData("real-tenant.auth0.com", "test-client-id")]
	[InlineData("real-tenant.auth0.com", "TEST-CLIENT-ID")]
	[InlineData("test.auth0.com", "test-client-id")]
	[InlineData("TEST.AUTH0.COM", "TEST-CLIENT-ID")]
	public void UsesPlaceholderWebAppLogin_ReturnsTrue_ForMissingOrKnownPlaceholderValues(string? domain, string? clientId)
	{
		// Act
		var result = Auth0ConfigurationHelper.UsesPlaceholderWebAppLogin(domain, clientId);

		// Assert
		result.Should().BeTrue();
	}

	[Theory]
	[InlineData(null, "real-client-id", "real-client-secret")]
	[InlineData("", "real-client-id", "real-client-secret")]
	[InlineData("real-tenant.auth0.com", null, "real-client-secret")]
	[InlineData("real-tenant.auth0.com", "", "real-client-secret")]
	[InlineData("real-tenant.auth0.com", "real-client-id", null)]
	[InlineData("real-tenant.auth0.com", "real-client-id", "")]
	[InlineData("test.auth0.com", "real-client-id", "real-client-secret")]
	[InlineData("TEST.AUTH0.COM", "real-client-id", "real-client-secret")]
	[InlineData("real-tenant.auth0.com", "test-client-id", "real-client-secret")]
	[InlineData("real-tenant.auth0.com", "TEST-CLIENT-ID", "real-client-secret")]
	[InlineData("real-tenant.auth0.com", "real-client-id", "test-client-secret")]
	public void UsesPlaceholderWebAppLogin_WithClientSecret_ReturnsTrue_ForMissingOrKnownPlaceholderValues(
			string? domain,
			string? clientId,
			string? clientSecret)
	{
		// Act
		var result = Auth0ConfigurationHelper.UsesPlaceholderWebAppLogin(domain, clientId, clientSecret);

		// Assert
		result.Should().BeTrue();
	}

	[Theory]
	[InlineData("tenant.auth0.com", "client-id", "client-secret")]
	[InlineData("TENANT.AUTH0.COM", "CLIENT-ID", "client-secret")]
	[InlineData("tenant.auth0.com", "client-id", "TEST-CLIENT-SECRET")]
	public void UsesPlaceholderWebAppLogin_WithClientSecret_ReturnsFalse_ForRealAuth0Settings(
			string domain,
			string clientId,
			string clientSecret)
	{
		// Act
		var result = Auth0ConfigurationHelper.UsesPlaceholderWebAppLogin(domain, clientId, clientSecret);

		// Assert
		result.Should().BeFalse();
	}

	[Theory]
	[InlineData(true, "test.auth0.com", "test-client-id", "test-client-secret", true)]
	[InlineData(true, "TEST.AUTH0.COM", "TEST-CLIENT-ID", "test-client-secret", true)]
	[InlineData(true, "tenant.auth0.com", "client-id", null, true)]
	[InlineData(true, "tenant.auth0.com", "client-id", "client-secret", false)]
	[InlineData(false, "test.auth0.com", "test-client-id", "test-client-secret", false)]
	[InlineData(false, "tenant.auth0.com", "client-id", null, false)]
	public void ShouldUseLocalTestLogin_WithClientSecret_ReturnsExpectedValue(
			bool isTestingEnvironment,
			string? domain,
			string? clientId,
			string? clientSecret,
			bool expected)
	{
		// Act
		var result = Auth0ConfigurationHelper.ShouldUseLocalTestLogin(
				isTestingEnvironment,
				domain,
				clientId,
				clientSecret);

		// Assert
		result.Should().Be(expected);
	}
}
