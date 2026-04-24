//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     DeleteBlogPostDomainCommandValidatorTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using FluentValidation.TestHelper;

using MyBlog.Web.Features.BlogPosts.Delete;

namespace Tests.Unit.Features.BlogPosts.Commands;

public class DeleteBlogPostDomainCommandValidatorTests
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
