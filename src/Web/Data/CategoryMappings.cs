//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CategoryMappings.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

namespace MyBlog.Web.Data;

internal static class CategoryMappings
{
	internal static CategoryDto ToDto(this Category category) =>
		new(category.Id.ToString(), category.Name, category.Description);
}
