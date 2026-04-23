//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CreateBlogPostDomainCommandValidatorTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using FluentValidation.TestHelper;

using MyBlog.Domain.Features.BlogPosts.Commands.CreateBlogPost;

namespace Tests.Unit.Features.BlogPosts.Commands;

public class CreateBlogPostDomainCommandValidatorTests
{
	private readonly CreateBlogPostCommandValidator _validator = new();

	[Fact]
	public void Validate_ValidCommand_PassesValidation()
	{
		var command = new CreateBlogPostCommand("Valid Title", "Valid Content", "Valid Author");

		var result = _validator.TestValidate(command);

		result.ShouldNotHaveAnyValidationErrors();
	}

	[Theory]
	[InlineData("")]
	[InlineData(null!)]
	public void Validate_EmptyTitle_FailsValidation(string? title)
	{
		var command = new CreateBlogPostCommand(title!, "Content", "Author");

		var result = _validator.TestValidate(command);

		result.ShouldHaveValidationErrorFor(x => x.Title);
	}

	[Fact]
	public void Validate_TitleExceedsMaxLength_FailsValidation()
	{
		var command = new CreateBlogPostCommand(new string('a', 201), "Content", "Author");

		var result = _validator.TestValidate(command);

		result.ShouldHaveValidationErrorFor(x => x.Title);
	}

	[Theory]
	[InlineData("")]
	[InlineData(null!)]
	public void Validate_EmptyContent_FailsValidation(string? content)
	{
		var command = new CreateBlogPostCommand("Title", content!, "Author");

		var result = _validator.TestValidate(command);

		result.ShouldHaveValidationErrorFor(x => x.Content);
	}

	[Theory]
	[InlineData("")]
	[InlineData(null!)]
	public void Validate_EmptyAuthor_FailsValidation(string? author)
	{
		var command = new CreateBlogPostCommand("Title", "Content", author!);

		var result = _validator.TestValidate(command);

		result.ShouldHaveValidationErrorFor(x => x.Author);
	}

	[Fact]
	public void Validate_AuthorExceedsMaxLength_FailsValidation()
	{
		var command = new CreateBlogPostCommand("Title", "Content", new string('a', 101));

		var result = _validator.TestValidate(command);

		result.ShouldHaveValidationErrorFor(x => x.Author);
	}
}
