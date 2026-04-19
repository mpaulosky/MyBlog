//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CreateBlogPostCommandValidator.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain
//=======================================================

using FluentValidation;

namespace MyBlog.Domain.Features.BlogPosts.Commands.CreateBlogPost;

public sealed class CreateBlogPostCommandValidator : AbstractValidator<CreateBlogPostCommand>
{
	public CreateBlogPostCommandValidator()
	{
		RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
		RuleFor(x => x.Content).NotEmpty();
		RuleFor(x => x.Author).NotEmpty().MaximumLength(100);
	}
}
