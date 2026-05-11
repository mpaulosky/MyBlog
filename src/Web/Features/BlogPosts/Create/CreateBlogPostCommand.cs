//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CreateBlogPostCommand.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using MyBlog.Domain.Abstractions;
using MyBlog.Domain.ValueObjects;

namespace MyBlog.Web.Features.BlogPosts.Create;

internal sealed record CreateBlogPostCommand(string Title, string Content, PostAuthor Author)
		: IRequest<Result<Guid>>;
