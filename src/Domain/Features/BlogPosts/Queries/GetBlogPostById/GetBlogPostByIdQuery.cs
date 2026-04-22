//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     GetBlogPostByIdQuery.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain
//=======================================================

using MediatR;

using MyBlog.Domain.Abstractions;
using MyBlog.Domain.Entities;

namespace MyBlog.Domain.Features.BlogPosts.Queries.GetBlogPostById;

public sealed record GetBlogPostByIdQuery(Guid Id) : IRequest<Result<BlogPost>>;
