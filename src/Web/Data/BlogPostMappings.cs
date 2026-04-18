//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     BlogPostMappings.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

namespace MyBlog.Web.Data;

internal static class BlogPostMappings
{
	internal static BlogPostDto ToDto(this BlogPost post) => new(
			post.Id, post.Title, post.Content, post.Author,
			post.CreatedAt, post.UpdatedAt, post.IsPublished);
}
