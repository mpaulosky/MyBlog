//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CreateBlogPostCommandValidator.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using FluentValidation;

namespace MyBlog.Web.Features.BlogPosts.Create;

internal sealed class CreateBlogPostCommandValidator : AbstractValidator<CreateBlogPostCommand>
{
	public CreateBlogPostCommandValidator()
	{
		RuleFor(x => x.Title)
			.NotEmpty().WithMessage("Title is required.")
			.MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

		RuleFor(x => x.Content)
			.NotEmpty().WithMessage("Content is required.");

		RuleFor(x => x.Author)
			.NotNull()
			.WithMessage("Author is required.");
		RuleFor(x => x.Author.Name)
			.NotEmpty()
			.WithMessage("Author name is required.")
			.When(x => x.Author is not null);
	}
}
