//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     EditCategoryCommand.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using MyBlog.Domain.Abstractions;

namespace MyBlog.Web.Features.Categories.Edit;

internal sealed record EditCategoryCommand(
	Guid Id,
	string Name,
	string Description) : IRequest<Result>;
