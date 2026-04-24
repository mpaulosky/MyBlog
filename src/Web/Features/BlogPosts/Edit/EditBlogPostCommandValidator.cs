//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     EditBlogPostCommandValidator.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using FluentValidation;

namespace MyBlog.Web.Features.BlogPosts.Edit;

internal sealed class EditBlogPostCommandValidator : AbstractValidator<EditBlogPostCommand>
{
	public EditBlogPostCommandValidator()
	{
		RuleFor(x => x.Id)
			.NotEmpty().WithMessage("Id is required.");

		RuleFor(x => x.Title)
			.NotEmpty().WithMessage("Title is required.")
			.MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

		RuleFor(x => x.Content)
			.NotEmpty().WithMessage("Content is required.");
	}
}
