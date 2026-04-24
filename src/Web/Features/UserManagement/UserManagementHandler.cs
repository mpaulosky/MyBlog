//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     UserManagementHandler.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using Auth0.ManagementApi;
using Auth0.ManagementApi.Users;

using MyBlog.Domain.Abstractions;

namespace MyBlog.Web.Features.UserManagement;

internal sealed class UserManagementHandler(
IConfiguration configuration,
IHttpClientFactory httpClientFactory)
: IRequestHandler<GetUsersWithRolesQuery, Result<IReadOnlyList<UserWithRolesDto>>>,
IRequestHandler<AssignRoleCommand, Result>,
IRequestHandler<RemoveRoleCommand, Result>,
IRequestHandler<GetAvailableRolesQuery, Result<IReadOnlyList<RoleDto>>>
{
public async Task<Result<IReadOnlyList<UserWithRolesDto>>> Handle(
GetUsersWithRolesQuery request, CancellationToken cancellationToken)
{
try
{
var client = await GetManagementClientAsync(cancellationToken).ConfigureAwait(false);
var usersPager = await client.Users.ListAsync(new ListUsersRequestParameters(), cancellationToken: cancellationToken).ConfigureAwait(false);
var result = new List<UserWithRolesDto>();
await foreach (var user in usersPager)
{
var rolesPager = await client.Users.Roles.ListAsync(
user.UserId ?? string.Empty, new ListUserRolesRequestParameters(), cancellationToken: cancellationToken).ConfigureAwait(false);
var roles = new List<string>();
await foreach (var role in rolesPager)
{
roles.Add(role.Name ?? string.Empty);
}
result.Add(new UserWithRolesDto(
user.UserId ?? string.Empty,
user.Email ?? string.Empty,
user.Name ?? user.Email ?? string.Empty,
roles));
}
return Result.Ok<IReadOnlyList<UserWithRolesDto>>(result);
}
catch (OperationCanceledException)
{
throw;
}
catch (InvalidOperationException ex)
{
return Result.Fail<IReadOnlyList<UserWithRolesDto>>(ex.Message);
}
catch (HttpRequestException ex)
{
return Result.Fail<IReadOnlyList<UserWithRolesDto>>(ex.Message);
}
#pragma warning disable CA1031 // Intentional: top-level handler converts unexpected failures to Result to keep UI stable
catch (Exception)
{
return Result.Fail<IReadOnlyList<UserWithRolesDto>>("An unexpected error occurred.");
}
#pragma warning restore CA1031
}

public async Task<Result> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
{
try
{
var client = await GetManagementClientAsync(cancellationToken).ConfigureAwait(false);
await client.Users.Roles.AssignAsync(
request.UserId,
new AssignUserRolesRequestContent { Roles = [request.RoleId] },
cancellationToken: cancellationToken).ConfigureAwait(false);
return Result.Ok();
}
catch (OperationCanceledException)
{
throw;
}
catch (InvalidOperationException ex)
{
return Result.Fail(ex.Message);
}
catch (HttpRequestException ex)
{
return Result.Fail(ex.Message);
}
#pragma warning disable CA1031 // Intentional: top-level handler converts unexpected failures to Result to keep UI stable
catch (Exception)
{
return Result.Fail("An unexpected error occurred.");
}
#pragma warning restore CA1031
}

public async Task<Result> Handle(RemoveRoleCommand request, CancellationToken cancellationToken)
{
try
{
var client = await GetManagementClientAsync(cancellationToken).ConfigureAwait(false);
await client.Users.Roles.DeleteAsync(
request.UserId,
new DeleteUserRolesRequestContent { Roles = [request.RoleId] },
cancellationToken: cancellationToken).ConfigureAwait(false);
return Result.Ok();
}
catch (OperationCanceledException)
{
throw;
}
catch (InvalidOperationException ex)
{
return Result.Fail(ex.Message);
}
catch (HttpRequestException ex)
{
return Result.Fail(ex.Message);
}
#pragma warning disable CA1031 // Intentional: top-level handler converts unexpected failures to Result to keep UI stable
catch (Exception)
{
return Result.Fail("An unexpected error occurred.");
}
#pragma warning restore CA1031
}

public async Task<Result<IReadOnlyList<RoleDto>>> Handle(GetAvailableRolesQuery request, CancellationToken cancellationToken)
{
try
{
var client = await GetManagementClientAsync(cancellationToken).ConfigureAwait(false);
var rolesPager = await client.Roles.ListAsync(new ListRolesRequestParameters(), cancellationToken: cancellationToken).ConfigureAwait(false);
var roles = new List<RoleDto>();
await foreach (var role in rolesPager)
{
roles.Add(new RoleDto(role.Id ?? string.Empty, role.Name ?? string.Empty));
}
return Result.Ok<IReadOnlyList<RoleDto>>(roles);
}
catch (OperationCanceledException)
{
throw;
}
catch (InvalidOperationException ex)
{
return Result.Fail<IReadOnlyList<RoleDto>>(ex.Message);
}
catch (HttpRequestException ex)
{
return Result.Fail<IReadOnlyList<RoleDto>>(ex.Message);
}
#pragma warning disable CA1031 // Intentional: top-level handler converts unexpected failures to Result to keep UI stable
catch (Exception)
{
return Result.Fail<IReadOnlyList<RoleDto>>("An unexpected error occurred.");
}
#pragma warning restore CA1031
}

private async Task<ManagementApiClient> GetManagementClientAsync(CancellationToken cancellationToken)
{
var domain = configuration["Auth0:ManagementApiDomain"]
?? throw new InvalidOperationException("Auth0:ManagementApiDomain not configured.");
var clientId = configuration["Auth0:ManagementApiClientId"]
?? throw new InvalidOperationException("Auth0:ManagementApiClientId not configured.");
var clientSecret = configuration["Auth0:ManagementApiClientSecret"]
?? throw new InvalidOperationException("Auth0:ManagementApiClientSecret not configured.");

var httpClient = httpClientFactory.CreateClient();
var tokenResponse = await httpClient.PostAsJsonAsync(
$"https://{domain}/oauth/token",
new
{
client_id = clientId,
client_secret = clientSecret,
audience = $"https://{domain}/api/v2/",
grant_type = "client_credentials"
}, cancellationToken).ConfigureAwait(false);
tokenResponse.EnsureSuccessStatusCode();
var tokenData = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken).ConfigureAwait(false);
return new ManagementApiClient(
token: tokenData!.AccessToken,
clientOptions: new ClientOptions { BaseUrl = $"https://{domain}/api/v2" });
}

private sealed class TokenResponse
{
public string AccessToken { get; init; } = string.Empty;
}
}
