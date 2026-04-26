//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     UserManagementHandlerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Unit.Tests
//=======================================================

using System.Net;

using Microsoft.Extensions.Configuration;

using MyBlog.Web.Features.UserManagement;

namespace Unit.Handlers;

public class UserManagementHandlerTests
{
	private readonly IConfiguration _config = Substitute.For<IConfiguration>();
	private readonly IHttpClientFactory _httpFactory = Substitute.For<IHttpClientFactory>();
	private readonly UserManagementHandler _handler;

	public UserManagementHandlerTests()
	{
		_config["Auth0:ManagementApiDomain"].Returns((string?)null);
		_handler = new UserManagementHandler(_config, _httpFactory);
	}

	// ── Domain missing ──────────────────────────────────────────────────────────────

	[Fact]
	public async Task Handle_GetUsersWithRoles_DomainMissing_ReturnsFailResult()
	{
		var result = await _handler.Handle(new GetUsersWithRolesQuery(), CancellationToken.None);

		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiDomain not configured");
	}

	[Fact]
	public async Task Handle_AssignRole_DomainMissing_ReturnsFailResult()
	{
		var result = await _handler.Handle(
			new AssignRoleCommand("user-1", "role-1"), CancellationToken.None);

		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiDomain not configured");
	}

	[Fact]
	public async Task Handle_RemoveRole_DomainMissing_ReturnsFailResult()
	{
		var result = await _handler.Handle(
			new RemoveRoleCommand("user-1", "role-1"), CancellationToken.None);

		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiDomain not configured");
	}

	[Fact]
	public async Task Handle_GetAvailableRoles_DomainMissing_ReturnsFailResult()
	{
		var result = await _handler.Handle(new GetAvailableRolesQuery(), CancellationToken.None);

		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiDomain not configured");
	}

	// ── ClientId missing ────────────────────────────────────────────────────────────────

	[Fact]
	public async Task Handle_GetUsersWithRoles_ClientIdMissing_ReturnsFailResult()
	{
		var handler = BuildHandlerClientIdMissing();

		var result = await handler.Handle(new GetUsersWithRolesQuery(), CancellationToken.None);

		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiClientId not configured");
	}

	[Fact]
	public async Task Handle_AssignRole_ClientIdMissing_ReturnsFailResult()
	{
		var handler = BuildHandlerClientIdMissing();

		var result = await handler.Handle(
			new AssignRoleCommand("user-1", "role-1"), CancellationToken.None);

		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiClientId not configured");
	}

	[Fact]
	public async Task Handle_RemoveRole_ClientIdMissing_ReturnsFailResult()
	{
		var handler = BuildHandlerClientIdMissing();

		var result = await handler.Handle(
			new RemoveRoleCommand("user-1", "role-1"), CancellationToken.None);

		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiClientId not configured");
	}

	[Fact]
	public async Task Handle_GetAvailableRoles_ClientIdMissing_ReturnsFailResult()
	{
		var handler = BuildHandlerClientIdMissing();

		var result = await handler.Handle(new GetAvailableRolesQuery(), CancellationToken.None);

		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiClientId not configured");
	}

	// ── ClientSecret missing ──────────────────────────────────────────────────────────────

	[Fact]
	public async Task Handle_GetUsersWithRoles_ClientSecretMissing_ReturnsFailResult()
	{
		var handler = BuildHandlerClientSecretMissing();

		var result = await handler.Handle(new GetUsersWithRolesQuery(), CancellationToken.None);

		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiClientSecret not configured");
	}

	[Fact]
	public async Task Handle_AssignRole_ClientSecretMissing_ReturnsFailResult()
	{
		var handler = BuildHandlerClientSecretMissing();

		var result = await handler.Handle(
			new AssignRoleCommand("user-1", "role-1"), CancellationToken.None);

		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiClientSecret not configured");
	}

	[Fact]
	public async Task Handle_RemoveRole_ClientSecretMissing_ReturnsFailResult()
	{
		var handler = BuildHandlerClientSecretMissing();

		var result = await handler.Handle(
			new RemoveRoleCommand("user-1", "role-1"), CancellationToken.None);

		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiClientSecret not configured");
	}

	[Fact]
	public async Task Handle_GetAvailableRoles_ClientSecretMissing_ReturnsFailResult()
	{
		var handler = BuildHandlerClientSecretMissing();

		var result = await handler.Handle(new GetAvailableRolesQuery(), CancellationToken.None);

		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiClientSecret not configured");
	}

	// ── HTTP token endpoint fails ────────────────────────────────────────────────────────────────────

	[Fact]
	public async Task Handle_GetUsersWithRoles_TokenEndpointFails_ReturnsFailResult()
	{
		var handler = BuildHandlerHttpFail(HttpStatusCode.InternalServerError);

		var result = await handler.Handle(new GetUsersWithRolesQuery(), CancellationToken.None);

		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("500");
	}

	[Fact]
	public async Task Handle_AssignRole_TokenEndpointFails_ReturnsFailResult()
	{
		var handler = BuildHandlerHttpFail(HttpStatusCode.InternalServerError);

		var result = await handler.Handle(
			new AssignRoleCommand("user-1", "role-1"), CancellationToken.None);

		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("500");
	}

	[Fact]
	public async Task Handle_RemoveRole_TokenEndpointFails_ReturnsFailResult()
	{
		var handler = BuildHandlerHttpFail(HttpStatusCode.InternalServerError);

		var result = await handler.Handle(
			new RemoveRoleCommand("user-1", "role-1"), CancellationToken.None);

		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("500");
	}

	[Fact]
	public async Task Handle_GetAvailableRoles_TokenEndpointFails_ReturnsFailResult()
	{
		var handler = BuildHandlerHttpFail(HttpStatusCode.InternalServerError);

		var result = await handler.Handle(new GetAvailableRolesQuery(), CancellationToken.None);

		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("500");
	}

	// ── helpers ───────────────────────────────────────────────────────────────────────────────

	private static UserManagementHandler BuildHandlerClientIdMissing()
	{
		var config = Substitute.For<IConfiguration>();
		config["Auth0:ManagementApiDomain"].Returns("test.auth0.com");
		config["Auth0:ManagementApiClientId"].Returns((string?)null);
		return new UserManagementHandler(config, Substitute.For<IHttpClientFactory>());
	}

	private static UserManagementHandler BuildHandlerClientSecretMissing()
	{
		var config = Substitute.For<IConfiguration>();
		config["Auth0:ManagementApiDomain"].Returns("test.auth0.com");
		config["Auth0:ManagementApiClientId"].Returns("test-client-id");
		config["Auth0:ManagementApiClientSecret"].Returns((string?)null);
		return new UserManagementHandler(config, Substitute.For<IHttpClientFactory>());
	}

	private static UserManagementHandler BuildHandlerHttpFail(HttpStatusCode statusCode)
	{
		var config = Substitute.For<IConfiguration>();
		config["Auth0:ManagementApiDomain"].Returns("test.auth0.com");
		config["Auth0:ManagementApiClientId"].Returns("test-client-id");
		config["Auth0:ManagementApiClientSecret"].Returns("test-client-secret");
		var httpFactory = Substitute.For<IHttpClientFactory>();
		httpFactory.CreateClient().Returns(new HttpClient(new StubHttpHandler(statusCode)));
		return new UserManagementHandler(config, httpFactory);
	}

	private sealed class StubHttpHandler(HttpStatusCode statusCode) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(new HttpResponseMessage(statusCode));
	}
}
