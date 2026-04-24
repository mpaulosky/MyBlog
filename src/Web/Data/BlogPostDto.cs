//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     BlogPostDto.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

namespace MyBlog.Web.Data;

internal sealed record BlogPostDto(
		Guid Id,
		string Title,
		string Content,
		string Author,
		DateTime CreatedAt,
		DateTime? UpdatedAt,
		bool IsPublished);
