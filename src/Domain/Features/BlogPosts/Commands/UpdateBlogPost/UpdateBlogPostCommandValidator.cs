//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     UpdateBlogPostCommandValidator.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain
//=======================================================

using FluentValidation;

namespace MyBlog.Domain.Features.BlogPosts.Commands.UpdateBlogPost;

public sealed class UpdateBlogPostCommandValidator : AbstractValidator<UpdateBlogPostCommand>
{
	public UpdateBlogPostCommandValidator()
	{
		RuleFor(x => x.Id).NotEmpty();
		RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
		RuleFor(x => x.Content).NotEmpty();
	}
}
