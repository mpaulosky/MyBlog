//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     UserManagementHandlerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Unit.Tests
//=======================================================

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

[Fact]
public async Task Handle_GetUsersWithRoles_ConfigMissing_ReturnsFailResult()
{
// Act
var result = await _handler.Handle(new GetUsersWithRolesQuery(), CancellationToken.None);

// Assert
result.Failure.Should().BeTrue();
result.Error.Should().Contain("Auth0:ManagementApiDomain not configured");
}

[Fact]
public async Task Handle_AssignRole_ConfigMissing_ReturnsFailResult()
{
// Act
var result = await _handler.Handle(
new AssignRoleCommand("user-1", "role-1"), CancellationToken.None);

// Assert
result.Failure.Should().BeTrue();
result.Error.Should().Contain("Auth0:ManagementApiDomain not configured");
}

[Fact]
public async Task Handle_RemoveRole_ConfigMissing_ReturnsFailResult()
{
// Act
var result = await _handler.Handle(
new RemoveRoleCommand("user-1", "role-1"), CancellationToken.None);

// Assert
result.Failure.Should().BeTrue();
result.Error.Should().Contain("Auth0:ManagementApiDomain not configured");
}

[Fact]
public async Task Handle_GetAvailableRoles_ConfigMissing_ReturnsFailResult()
{
// Act
var result = await _handler.Handle(new GetAvailableRolesQuery(), CancellationToken.None);

// Assert
result.Failure.Should().BeTrue();
result.Error.Should().Contain("Auth0:ManagementApiDomain not configured");
}
}
