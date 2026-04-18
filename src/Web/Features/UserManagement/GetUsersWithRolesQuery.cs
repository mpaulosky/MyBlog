//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     GetUsersWithRolesQuery.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

namespace MyBlog.Web.Features.UserManagement;

public sealed record GetUsersWithRolesQuery : IRequest<Result<IReadOnlyList<UserWithRolesDto>>>;

public sealed record UserWithRolesDto(string UserId, string Email, string Name, IReadOnlyList<string> Roles);
