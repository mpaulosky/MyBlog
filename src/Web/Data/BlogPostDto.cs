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
		string Id,
		string Title,
		string Content,
		string AuthorId,
		string AuthorName,
		string AuthorEmail,
		IReadOnlyList<string> AuthorRoles,
		DateTime CreatedAt,
		DateTime? UpdatedAt,
		bool IsPublished,
		string? CategoryId);
