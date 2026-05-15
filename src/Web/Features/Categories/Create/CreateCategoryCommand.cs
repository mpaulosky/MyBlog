//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CreateCategoryCommand.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using MyBlog.Domain.Abstractions;

namespace MyBlog.Web.Features.Categories.Create;

internal sealed record CreateCategoryCommand(
	string Name,
	string Description) : IRequest<Result<Guid>>;
