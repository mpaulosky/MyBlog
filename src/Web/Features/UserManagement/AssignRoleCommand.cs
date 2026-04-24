//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     AssignRoleCommand.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using MyBlog.Domain.Abstractions;

namespace MyBlog.Web.Features.UserManagement;

public sealed record AssignRoleCommand(string UserId, string RoleId) : IRequest<Result>;
