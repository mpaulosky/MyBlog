//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CreateBlogPostCommandValidatorTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using MyBlog.Web.Features.BlogPosts.Create;

namespace Web.Features.BlogPosts.Commands;

public class CreateBlogPostCommandValidatorTests
{
	private readonly CreateBlogPostCommandValidator _sut = new();

	[Fact]
	public void Validate_ValidCommand_ReturnsNoErrors()
	{
		// Arrange
		var command = new CreateBlogPostCommand("Valid Title", "Valid Content", "Valid Author");

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeTrue();
	}

	[Theory]
	[InlineData("", "Content", "Author")]
	[InlineData("Title", "", "Author")]
	[InlineData("Title", "Content", "")]
	public void Validate_MissingRequiredFields_ReturnsErrors(string title, string content, string author)
	{
		// Arrange
		var command = new CreateBlogPostCommand(title, content, author);

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_TitleExceedsMaxLength_ReturnsError()
	{
		// Arrange
		var command = new CreateBlogPostCommand(new string('A', 201), "Content", "Author");

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().ContainSingle(e => e.PropertyName == "Title");
	}

	[Fact]
	public void Validate_AuthorExceedsMaxLength_ReturnsError()
	{
		// Arrange
		var command = new CreateBlogPostCommand("Title", "Content", new string('A', 101));

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().ContainSingle(e => e.PropertyName == "Author");
	}

	[Fact]
	public void Validate_TitleAtMaxLength_ReturnsNoErrors()
	{
		// Arrange
		var command = new CreateBlogPostCommand(new string('A', 200), "Content", "Author");

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_AuthorAtMaxLength_ReturnsNoErrors()
	{
		// Arrange
		var command = new CreateBlogPostCommand("Title", "Content", new string('A', 100));

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_WhitespaceTitle_ReturnsError()
	{
		// Arrange
		var command = new CreateBlogPostCommand("   ", "Content", "Author");

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Title");
	}

	[Fact]
	public void Validate_WhitespaceAuthor_ReturnsError()
	{
		// Arrange
		var command = new CreateBlogPostCommand("Title", "Content", "   ");

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Author");
	}

	[Fact]
	public void Validate_WhitespaceContent_ReturnsError()
	{
		// Arrange
		var command = new CreateBlogPostCommand("Title", "   ", "Author");

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Content");
	}
}
