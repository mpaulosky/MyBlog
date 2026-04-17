using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Auth0.ManagementApi.Paging;
using MediatR;
using MyBlog.Domain.Common;
using System.Net.Http.Json;

namespace MyBlog.Web.Features.UserManagement;

public sealed class UserManagementHandler(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory)
    : IRequestHandler<GetUsersWithRolesQuery, Result<IReadOnlyList<UserWithRolesDto>>>,
      IRequestHandler<AssignRoleCommand, Result>,
      IRequestHandler<RemoveRoleCommand, Result>,
      IRequestHandler<GetAvailableRolesQuery, Result<IReadOnlyList<RoleDto>>>
{
    public async Task<Result<IReadOnlyList<UserWithRolesDto>>> Handle(
        GetUsersWithRolesQuery request, CancellationToken ct)
    {
        try
        {
            var client = await GetManagementClientAsync(ct);
            var users = await client.Users.GetAllAsync(new GetUsersRequest(), new PaginationInfo(), ct);
            var result = new List<UserWithRolesDto>();
            foreach (var user in users)
            {
                var roles = await client.Users.GetRolesAsync(user.UserId, new PaginationInfo(), ct);
                result.Add(new UserWithRolesDto(
                    user.UserId,
                    user.Email ?? string.Empty,
                    user.FullName ?? user.Email ?? string.Empty,
                    roles.Select(r => r.Name ?? string.Empty).ToList()));
            }
            return Result<IReadOnlyList<UserWithRolesDto>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<UserWithRolesDto>>.Failure(ex.Message);
        }
    }

    public async Task<Result> Handle(AssignRoleCommand request, CancellationToken ct)
    {
        try
        {
            var client = await GetManagementClientAsync(ct);
            await client.Users.AssignRolesAsync(request.UserId,
                new AssignRolesRequest { Roles = [request.RoleId] }, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> Handle(RemoveRoleCommand request, CancellationToken ct)
    {
        try
        {
            var client = await GetManagementClientAsync(ct);
            await client.Users.RemoveRolesAsync(request.UserId,
                new AssignRolesRequest { Roles = [request.RoleId] }, ct);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result<IReadOnlyList<RoleDto>>> Handle(GetAvailableRolesQuery request, CancellationToken ct)
    {
        try
        {
            var client = await GetManagementClientAsync(ct);
            var roles = await client.Roles.GetAllAsync(new GetRolesRequest(), new PaginationInfo(), ct);
            return Result<IReadOnlyList<RoleDto>>.Success(
                roles.Select(r => new RoleDto(r.Id ?? string.Empty, r.Name ?? string.Empty)).ToList());
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<RoleDto>>.Failure(ex.Message);
        }
    }

    private async Task<ManagementApiClient> GetManagementClientAsync(CancellationToken ct)
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
            }, ct);
        tokenResponse.EnsureSuccessStatusCode();
        var tokenData = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(ct);
        return new ManagementApiClient(tokenData!.AccessToken, domain);
    }

    private sealed class TokenResponse
    {
        public string AccessToken { get; init; } = string.Empty;
    }
}
