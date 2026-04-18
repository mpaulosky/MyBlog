// ============================================
// Copyright (c) 2025. All rights reserved.
// File Name :     TestAuthorizationService.cs
// Company :       mpaulosky
// Author :        mpaulosky
// Solution Name : MyBlog
// Project Name :  Unit.Tests
// =============================================
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace MyBlog.Unit.Tests.Testing;

internal sealed class TestAuthorizationService : IAuthorizationService
{
    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
    {
        foreach (var requirement in requirements)
        {
            switch (requirement)
            {
                case DenyAnonymousAuthorizationRequirement:
                    if (user.Identity?.IsAuthenticated != true)
                    {
                        return Task.FromResult(AuthorizationResult.Failed());
                    }
                    break;

                case RolesAuthorizationRequirement rolesRequirement:
                    if (!rolesRequirement.AllowedRoles.Any(user.IsInRole))
                    {
                        return Task.FromResult(AuthorizationResult.Failed());
                    }
                    break;
            }
        }

        return Task.FromResult(AuthorizationResult.Success());
    }

    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
    {
        // Policy evaluation not implemented; grants access to authenticated users only.
        return Task.FromResult(user.Identity?.IsAuthenticated == true
            ? AuthorizationResult.Success()
            : AuthorizationResult.Failed());
    }
}
