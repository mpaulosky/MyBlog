//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     DeleteBlogPostCommandValidatorTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Unit.Tests
//=======================================================

using MyBlog.Web.Features.BlogPosts.Delete;

namespace MyBlog.Unit.Tests.Features.BlogPosts.Commands;

public class DeleteBlogPostCommandValidatorTests
{
	private readonly DeleteBlogPostCommandValidator _sut = new();

	[Fact]
	public void Validate_ValidId_ReturnsNoErrors()
	{
		var command = new DeleteBlogPostCommand(Guid.NewGuid());
		var result = _sut.Validate(command);
		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_EmptyGuid_ReturnsError()
	{
		var command = new DeleteBlogPostCommand(Guid.Empty);
		var result = _sut.Validate(command);
		result.IsValid.Should().BeFalse();
		result.Errors.Should().ContainSingle(e => e.PropertyName == "Id");
	}

	[Fact]
	public void Validate_EmptyGuid_ReturnsRequiredMessage()
	{
		var command = new DeleteBlogPostCommand(Guid.Empty);
		var result = _sut.Validate(command);
		result.Errors.Should().ContainSingle(e =>
			e.PropertyName == "Id" && e.ErrorMessage == "Id is required.");
	}
}
