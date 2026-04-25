//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CreateBlogPostCommand.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using MyBlog.Domain.Abstractions;

namespace MyBlog.Web.Features.BlogPosts.Create;

internal sealed record CreateBlogPostCommand(string Title, string Content, string Author)
		: IRequest<Result<Guid>>;
