//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CategoryDto.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

namespace MyBlog.Web.Data;

internal sealed record CategoryDto(ObjectId Id, string Name, string Description);
