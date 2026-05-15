//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     DeleteCategoryCommandValidator.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using FluentValidation;

namespace MyBlog.Web.Features.Categories.Delete;

internal sealed class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
{
	public DeleteCategoryCommandValidator()
	{
		RuleFor(x => x.Id)
			.NotEmpty().WithMessage("Id is required.");
	}
}
