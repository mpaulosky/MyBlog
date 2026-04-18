//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     GetAvailableRolesQuery.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

namespace MyBlog.Web.Features.UserManagement;

public sealed record GetAvailableRolesQuery : IRequest<Result<IReadOnlyList<RoleDto>>>;

public sealed record RoleDto(string Id, string Name);
