//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     EditBlogPostCommand.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

namespace MyBlog.Web.Features.BlogPosts.Edit;

public sealed record EditBlogPostCommand(Guid Id, string Title, string Content) : IRequest<Result>;
