//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     UpdateBlogPostCommandHandler.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain
//=======================================================

using MediatR;

using MyBlog.Domain.Abstractions;
using MyBlog.Domain.Interfaces;

namespace MyBlog.Domain.Features.BlogPosts.Commands.UpdateBlogPost;

public sealed class UpdateBlogPostCommandHandler : IRequestHandler<UpdateBlogPostCommand, Result>
{
	private readonly IBlogPostRepository _repository;

	public UpdateBlogPostCommandHandler(IBlogPostRepository repository)
	{
		_repository = repository;
	}

	public async Task<Result> Handle(UpdateBlogPostCommand request, CancellationToken cancellationToken)
	{
		var post = await _repository.GetByIdAsync(request.Id, cancellationToken);
		if (post is null)
			return Result.Fail("Blog post not found.", ResultErrorCode.NotFound);

		post.Update(request.Title, request.Content);
		await _repository.UpdateAsync(post, cancellationToken);
		return Result.Ok();
	}
}
