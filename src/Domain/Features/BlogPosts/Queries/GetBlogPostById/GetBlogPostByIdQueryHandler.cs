//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     GetBlogPostByIdQueryHandler.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain
//=======================================================

using Domain.Abstractions;

using MediatR;

using MyBlog.Domain.Entities;
using MyBlog.Domain.Interfaces;

namespace MyBlog.Domain.Features.BlogPosts.Queries.GetBlogPostById;

public sealed class GetBlogPostByIdQueryHandler : IRequestHandler<GetBlogPostByIdQuery, Result<BlogPost>>
{
	private readonly IBlogPostRepository _repository;

	public GetBlogPostByIdQueryHandler(IBlogPostRepository repository)
	{
		_repository = repository;
	}

	public async Task<Result<BlogPost>> Handle(GetBlogPostByIdQuery request, CancellationToken cancellationToken)
	{
		var post = await _repository.GetByIdAsync(request.Id, cancellationToken);
		if (post is null)
			return Result.Fail<BlogPost>("Blog post not found.", ResultErrorCode.NotFound);
		return Result.Ok(post);
	}
}
