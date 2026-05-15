//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     DeleteCategoryCommandValidatorTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using MyBlog.Web.Features.Categories.Delete;

namespace Web.Features.Categories.Commands;

public class DeleteCategoryCommandValidatorTests
{
	private readonly DeleteCategoryCommandValidator _validator = new();

	[Fact]
	public void Validate_ValidId_ReturnsNoErrors()
	{
		// Arrange
		var command = new DeleteCategoryCommand(Guid.NewGuid());

		// Act
		var result = _validator.Validate(command);

		// Assert
		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_EmptyGuid_ReturnsIdError()
	{
		// Arrange
		var command = new DeleteCategoryCommand(Guid.Empty);

		// Act
		var result = _validator.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Id");
	}

	[Fact]
	public void Validate_EmptyGuid_ReturnsRequiredMessage()
	{
		// Arrange
		var command = new DeleteCategoryCommand(Guid.Empty);

		// Act
		var result = _validator.Validate(command);

		// Assert
		result.Errors.Should().Contain(e => e.ErrorMessage == "Id is required.");
	}
}
