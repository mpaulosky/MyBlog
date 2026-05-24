//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     EditBlogPostCommand.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using MyBlog.Domain.Abstractions;

namespace MyBlog.Web.Features.BlogPosts.Edit;

internal sealed record EditBlogPostCommand(
	ObjectId Id,
	string Title,
	string Content,
	string CallerUserId,
	bool CallerIsAdmin,
	bool? IsPublished = null,
	ObjectId? CategoryId = null) : IRequest<Result>;
