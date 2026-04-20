//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     DeleteBlogPostCommandHandler.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain
//=======================================================

using Domain.Abstractions;

using MediatR;

using MyBlog.Domain.Interfaces;

namespace MyBlog.Domain.Features.BlogPosts.Commands.DeleteBlogPost;

public sealed class DeleteBlogPostCommandHandler : IRequestHandler<DeleteBlogPostCommand, Result>
{
	private readonly IBlogPostRepository _repository;

	public DeleteBlogPostCommandHandler(IBlogPostRepository repository)
	{
		_repository = repository;
	}

	public async Task<Result> Handle(DeleteBlogPostCommand request, CancellationToken cancellationToken)
	{
		await _repository.DeleteAsync(request.Id, cancellationToken);
		return Result.Ok();
	}
}
