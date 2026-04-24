//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     GetBlogPostByIdQuery.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using MyBlog.Domain.Abstractions;

namespace MyBlog.Web.Features.BlogPosts.Edit;

internal sealed record GetBlogPostByIdQuery(Guid Id) : IRequest<Result<BlogPostDto?>>;
