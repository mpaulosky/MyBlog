//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     GetBlogPostByIdQuery.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

namespace MyBlog.Web.Features.BlogPosts.Edit;

public sealed record GetBlogPostByIdQuery(Guid Id) : IRequest<Result<BlogPostDto?>>;
