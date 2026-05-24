//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     EditCategoryCommandValidatorTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using MyBlog.Web.Features.Categories.Edit;

namespace Web.Features.Categories.Commands;

public class EditCategoryCommandValidatorTests
{
	private readonly EditCategoryCommandValidator _validator = new();

	[Fact]
	public void Validate_ValidCommand_ReturnsNoErrors()
	{
		// Arrange
		var command = new EditCategoryCommand(ObjectId.GenerateNewId(), "Technology", "All about tech.");

		// Act
		var result = _validator.Validate(command);

		// Assert
		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_EmptyId_ReturnsIdError()
	{
		// Arrange
		var command = new EditCategoryCommand(ObjectId.Empty, "Technology", "All about tech.");

		// Act
		var result = _validator.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Id");
	}

	[Fact]
	public void Validate_EmptyName_ReturnsNameError()
	{
		// Arrange
		var command = new EditCategoryCommand(ObjectId.GenerateNewId(), "", "All about tech.");

		// Act
		var result = _validator.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Name");
	}

	[Fact]
	public void Validate_EmptyDescription_ReturnsDescriptionError()
	{
		// Arrange
		var command = new EditCategoryCommand(ObjectId.GenerateNewId(), "Technology", "");

		// Act
		var result = _validator.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Description");
	}

	[Fact]
	public void Validate_NameExceedsMaxLength_ReturnsNameError()
	{
		// Arrange
		var longName = new string('x', 101);
		var command = new EditCategoryCommand(ObjectId.GenerateNewId(), longName, "All about tech.");

		// Act
		var result = _validator.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Name"
			&& e.ErrorMessage == "Name must not exceed 100 characters.");
	}

	[Fact]
	public void Validate_DescriptionExceedsMaxLength_ReturnsDescriptionError()
	{
		// Arrange
		var longDesc = new string('x', 501);
		var command = new EditCategoryCommand(ObjectId.GenerateNewId(), "Technology", longDesc);

		// Act
		var result = _validator.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Description"
			&& e.ErrorMessage == "Description must not exceed 500 characters.");
	}
}
