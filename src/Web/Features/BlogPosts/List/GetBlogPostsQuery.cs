//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     GetBlogPostsQuery.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

namespace MyBlog.Web.Features.BlogPosts.List;

public sealed record GetBlogPostsQuery : IRequest<Result<IReadOnlyList<BlogPostDto>>>;
