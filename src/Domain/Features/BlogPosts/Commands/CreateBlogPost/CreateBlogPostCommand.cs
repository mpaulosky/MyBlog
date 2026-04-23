//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CreateBlogPostCommand.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain
//=======================================================

using MediatR;

using MyBlog.Domain.Abstractions;

namespace MyBlog.Domain.Features.BlogPosts.Commands.CreateBlogPost;

public sealed record CreateBlogPostCommand(string Title, string Content, string Author) : IRequest<Result<Guid>>;
