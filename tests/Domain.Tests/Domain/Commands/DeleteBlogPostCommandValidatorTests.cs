//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     DeleteBlogPostCommandValidatorTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain.Tests
//=======================================================

using FluentValidation.TestHelper;

using MyBlog.Domain.Features.BlogPosts.Commands.DeleteBlogPost;

namespace Domain.Domain.Commands;

public class DeleteBlogPostCommandValidatorTests
{
	private readonly DeleteBlogPostCommandValidator _validator = new();

	[Fact]
	public void Validate_ValidId_PassesValidation()
	{
		var command = new DeleteBlogPostCommand(Guid.NewGuid());

		var result = _validator.TestValidate(command);

		result.ShouldNotHaveAnyValidationErrors();
	}

	[Fact]
	public void Validate_EmptyId_FailsValidation()
	{
		var command = new DeleteBlogPostCommand(Guid.Empty);

		var result = _validator.TestValidate(command);

		result.ShouldHaveValidationErrorFor(x => x.Id);
	}
}
