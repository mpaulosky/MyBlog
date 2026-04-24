//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     TestAuthorizationService.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests.Bunit
//=======================================================

using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Web.Testing;

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
		// Policy evaluation isn't implemented; grants access to authenticated users only.
		return Task.FromResult(user.Identity?.IsAuthenticated == true
				? AuthorizationResult.Success()
				: AuthorizationResult.Failed());
	}
}
