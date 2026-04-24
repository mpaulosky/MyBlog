//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     DeleteBlogPostCommandValidator.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using FluentValidation;

namespace MyBlog.Web.Features.BlogPosts.Delete;

internal sealed class DeleteBlogPostCommandValidator : AbstractValidator<DeleteBlogPostCommand>
{
	public DeleteBlogPostCommandValidator()
	{
		RuleFor(x => x.Id)
			.NotEmpty().WithMessage("Id is required.");
	}
}
