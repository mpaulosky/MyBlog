//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     GetCategoryByIdQuery.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using MyBlog.Domain.Abstractions;

namespace MyBlog.Web.Features.Categories.GetById;

internal sealed record GetCategoryByIdQuery(ObjectId Id) : IRequest<Result<CategoryDto?>>;
