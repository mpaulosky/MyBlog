//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     DeleteBlogPostCommandValidator.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain
//=======================================================

using FluentValidation;

namespace MyBlog.Domain.Features.BlogPosts.Commands.DeleteBlogPost;

public sealed class DeleteBlogPostCommandValidator : AbstractValidator<DeleteBlogPostCommand>
{
	public DeleteBlogPostCommandValidator()
	{
		RuleFor(x => x.Id).NotEmpty();
	}
}
