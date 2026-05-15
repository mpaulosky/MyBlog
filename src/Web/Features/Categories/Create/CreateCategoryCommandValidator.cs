//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CreateCategoryCommandValidator.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using FluentValidation;

namespace MyBlog.Web.Features.Categories.Create;

internal sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
	public CreateCategoryCommandValidator()
	{
		RuleFor(x => x.Name)
			.NotEmpty().WithMessage("Name is required.")
			.MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

		RuleFor(x => x.Description)
			.NotEmpty().WithMessage("Description is required.")
			.MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
	}
}
