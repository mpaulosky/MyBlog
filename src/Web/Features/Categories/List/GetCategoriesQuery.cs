//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     GetCategoriesQuery.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using MyBlog.Domain.Abstractions;

namespace MyBlog.Web.Features.Categories.List;

internal sealed record GetCategoriesQuery : IRequest<Result<IReadOnlyList<CategoryDto>>>;
