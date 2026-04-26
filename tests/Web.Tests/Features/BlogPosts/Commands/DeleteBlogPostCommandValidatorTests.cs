//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     DeleteBlogPostCommandValidatorTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using MyBlog.Web.Features.BlogPosts.Delete;

namespace Web.Features.BlogPosts.Commands;

public class DeleteBlogPostCommandValidatorTests
{
	private readonly DeleteBlogPostCommandValidator _sut = new();

	[Fact]
	public void Validate_ValidId_ReturnsNoErrors()
	{
		// Arrange
		var command = new DeleteBlogPostCommand(Guid.NewGuid());

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_EmptyGuid_ReturnsError()
	{
		// Arrange
		var command = new DeleteBlogPostCommand(Guid.Empty);

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().ContainSingle(e => e.PropertyName == "Id");
	}

	[Fact]
	public void Validate_EmptyGuid_ReturnsRequiredMessage()
	{
		// Arrange
		var command = new DeleteBlogPostCommand(Guid.Empty);

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.Errors.Should().ContainSingle(e =>
			e.PropertyName == "Id" && e.ErrorMessage == "Id is required.");
	}
}
