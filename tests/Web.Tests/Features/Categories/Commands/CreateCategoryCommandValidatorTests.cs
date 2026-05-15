//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CreateCategoryCommandValidatorTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using MyBlog.Web.Features.Categories.Create;

namespace Web.Features.Categories.Commands;

public class CreateCategoryCommandValidatorTests
{
	private readonly CreateCategoryCommandValidator _sut = new();

	[Fact]
	public void Validate_ValidCommand_ReturnsNoErrors()
	{
		// Arrange
		var command = new CreateCategoryCommand("Technology", "Posts about technology topics.");

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeTrue();
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void Validate_EmptyOrWhitespaceName_ReturnsNameError(string name)
	{
		// Arrange
		var command = new CreateCategoryCommand(name, "Valid description.");

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Name");
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void Validate_EmptyOrWhitespaceDescription_ReturnsDescriptionError(string description)
	{
		// Arrange
		var command = new CreateCategoryCommand("Tech", description);

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Description");
	}

	[Fact]
	public void Validate_NameExceedsMaxLength_ReturnsNameError()
	{
		// Arrange
		var command = new CreateCategoryCommand(new string('A', 101), "Valid description.");

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().ContainSingle(e => e.PropertyName == "Name");
	}

	[Fact]
	public void Validate_NameAtMaxLength_ReturnsNoErrors()
	{
		// Arrange
		var command = new CreateCategoryCommand(new string('A', 100), "Valid description.");

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_DescriptionExceedsMaxLength_ReturnsDescriptionError()
	{
		// Arrange
		var command = new CreateCategoryCommand("Tech", new string('A', 501));

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().ContainSingle(e => e.PropertyName == "Description");
	}

	[Fact]
	public void Validate_DescriptionAtMaxLength_ReturnsNoErrors()
	{
		// Arrange
		var command = new CreateCategoryCommand("Tech", new string('A', 500));

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_BothFieldsEmpty_ReturnsTwoErrors()
	{
		// Arrange
		var command = new CreateCategoryCommand("", "");

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().HaveCountGreaterThan(1);
	}

	[Fact]
	public void Validate_NameRequired_ErrorMessageIsCorrect()
	{
		// Arrange
		var command = new CreateCategoryCommand("", "Valid description.");

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.Errors.Should().Contain(e =>
			e.PropertyName == "Name" && e.ErrorMessage == "Name is required.");
	}

	[Fact]
	public void Validate_DescriptionRequired_ErrorMessageIsCorrect()
	{
		// Arrange
		var command = new CreateCategoryCommand("Tech", "");

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.Errors.Should().Contain(e =>
			e.PropertyName == "Description" && e.ErrorMessage == "Description is required.");
	}
}
