//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     EditCategoryCommandValidator.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using FluentValidation;

namespace MyBlog.Web.Features.Categories.Edit;

internal sealed class EditCategoryCommandValidator : AbstractValidator<EditCategoryCommand>
{
	public EditCategoryCommandValidator()
	{
		RuleFor(x => x.Id)
			.NotEmpty().WithMessage("Id is required.");

		RuleFor(x => x.Name)
			.NotEmpty().WithMessage("Name is required.")
			.MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

		RuleFor(x => x.Description)
			.NotEmpty().WithMessage("Description is required.")
			.MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
	}
}
