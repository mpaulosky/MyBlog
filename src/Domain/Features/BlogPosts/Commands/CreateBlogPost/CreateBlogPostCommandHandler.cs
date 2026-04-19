//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CreateBlogPostCommandHandler.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain
//=======================================================

using Domain.Abstractions;

using MediatR;

using MyBlog.Domain.Entities;
using MyBlog.Domain.Interfaces;

namespace MyBlog.Domain.Features.BlogPosts.Commands.CreateBlogPost;

public sealed class CreateBlogPostCommandHandler : IRequestHandler<CreateBlogPostCommand, Result<Guid>>
{
	private readonly IBlogPostRepository _repository;

	public CreateBlogPostCommandHandler(IBlogPostRepository repository)
	{
		_repository = repository;
	}

	public async Task<Result<Guid>> Handle(CreateBlogPostCommand request, CancellationToken cancellationToken)
	{
		var post = BlogPost.Create(request.Title, request.Content, request.Author);
		await _repository.AddAsync(post, cancellationToken);
		return Result.Ok(post.Id);
	}
}
