//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     DeleteBlogPostCommand.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain
//=======================================================

using MediatR;

using MyBlog.Domain.Abstractions;

namespace MyBlog.Domain.Features.BlogPosts.Commands.DeleteBlogPost;

public sealed record DeleteBlogPostCommand(Guid Id) : IRequest<Result>;
