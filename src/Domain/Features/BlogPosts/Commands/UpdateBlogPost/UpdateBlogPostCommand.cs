//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     UpdateBlogPostCommand.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain
//=======================================================

using Domain.Abstractions;

using MediatR;

namespace MyBlog.Domain.Features.BlogPosts.Commands.UpdateBlogPost;

public sealed record UpdateBlogPostCommand(Guid Id, string Title, string Content) : IRequest<Result>;
