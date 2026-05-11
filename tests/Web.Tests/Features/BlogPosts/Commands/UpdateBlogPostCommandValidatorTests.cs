//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     UpdateBlogPostCommandValidatorTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using FluentValidation.TestHelper;

using MyBlog.Web.Features.BlogPosts.Edit;

namespace Tests.Unit.Features.BlogPosts.Commands;

public class UpdateBlogPostCommandValidatorTests
{
	private readonly EditBlogPostCommandValidator _validator = new();

	[Fact]
	public void Validate_ValidCommand_PassesValidation()
	{
		var command = new EditBlogPostCommand(Guid.NewGuid(), "Valid Title", "Valid Content", string.Empty, false);

		var result = _validator.TestValidate(command);

		result.ShouldNotHaveAnyValidationErrors();
	}

	[Fact]
	public void Validate_EmptyId_FailsValidation()
	{
		var command = new EditBlogPostCommand(Guid.Empty, "Title", "Content", string.Empty, false);

		var result = _validator.TestValidate(command);

		result.ShouldHaveValidationErrorFor(x => x.Id);
	}

	[Theory]
	[InlineData("")]
	[InlineData(null!)]
	public void Validate_EmptyTitle_FailsValidation(string? title)
	{
		var command = new EditBlogPostCommand(Guid.NewGuid(), title!, "Content", string.Empty, false);

		var result = _validator.TestValidate(command);

		result.ShouldHaveValidationErrorFor(x => x.Title);
	}

	[Fact]
	public void Validate_TitleExceedsMaxLength_FailsValidation()
	{
		var command = new EditBlogPostCommand(Guid.NewGuid(), new string('a', 201), "Content", string.Empty, false);

		var result = _validator.TestValidate(command);

		result.ShouldHaveValidationErrorFor(x => x.Title);
	}

	[Theory]
	[InlineData("")]
	[InlineData(null!)]
	public void Validate_EmptyContent_FailsValidation(string? content)
	{
		var command = new EditBlogPostCommand(Guid.NewGuid(), "Title", content!, string.Empty, false);

		var result = _validator.TestValidate(command);

		result.ShouldHaveValidationErrorFor(x => x.Content);
	}
}
