//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CreateBlogPostHandler.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using MyBlog.Domain.Abstractions;
using MyBlog.Web.Infrastructure.Caching;

namespace MyBlog.Web.Features.BlogPosts.Create;

public sealed class CreateBlogPostHandler(
IBlogPostRepository repo,
IBlogPostCacheService cache) : IRequestHandler<CreateBlogPostCommand, Result<Guid>>
{
public async Task<Result<Guid>> Handle(CreateBlogPostCommand request, CancellationToken cancellationToken)
{
try
{
var post = BlogPost.Create(request.Title, request.Content, request.Author);
await repo.AddAsync(post, cancellationToken);
await cache.InvalidateAllAsync(cancellationToken);
return Result.Ok<Guid>(post.Id);
}
catch (Exception ex)
{
return Result.Fail<Guid>(ex.Message);
}
}
}
