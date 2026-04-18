//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     RemoveRoleCommand.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

namespace MyBlog.Web.Features.UserManagement;

public sealed record RemoveRoleCommand(string UserId, string RoleId) : IRequest<Result>;
