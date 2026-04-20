//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     GetAllBlogPostsQueryHandler.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain
//=======================================================

using Domain.Abstractions;

using MediatR;

using MyBlog.Domain.Entities;
using MyBlog.Domain.Interfaces;

namespace MyBlog.Domain.Features.BlogPosts.Queries.GetAllBlogPosts;

public sealed class GetAllBlogPostsQueryHandler : IRequestHandler<GetAllBlogPostsQuery, Result<IReadOnlyList<BlogPost>>>
{
	private readonly IBlogPostRepository _repository;

	public GetAllBlogPostsQueryHandler(IBlogPostRepository repository)
	{
		_repository = repository;
	}

	public async Task<Result<IReadOnlyList<BlogPost>>> Handle(GetAllBlogPostsQuery request, CancellationToken cancellationToken)
	{
		var posts = await _repository.GetAllAsync(cancellationToken);
		return Result.Ok(posts);
	}
}
