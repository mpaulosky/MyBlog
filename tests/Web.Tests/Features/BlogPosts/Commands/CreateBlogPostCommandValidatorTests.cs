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

	private static readonly PostAuthor ValidAuthor = new("id", "Valid Author", "author@example.com", []);

	[Fact]
	public void Validate_ValidCommand_ReturnsNoErrors()
	{
		// Arrange
		var command = new CreateBlogPostCommand("Valid Title", "Valid Content", ValidAuthor);

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_MissingTitle_ReturnsError()
	{
		// Arrange
		var command = new CreateBlogPostCommand("", "Content", ValidAuthor);

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_MissingContent_ReturnsError()
	{
		// Arrange
		var command = new CreateBlogPostCommand("Title", "", ValidAuthor);

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_NullAuthor_ReturnsError()
	{
		// Arrange
		var command = new CreateBlogPostCommand("Title", "Content", null!);

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public void Validate_AuthorWithEmptyName_ReturnsError()
	{
		// Arrange
		var command = new CreateBlogPostCommand("Title", "Content", new PostAuthor("", "", "", []));

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Author.Name");
	}

	[Fact]
	public void Validate_TitleExceedsMaxLength_ReturnsError()
	{
		// Arrange
		var command = new CreateBlogPostCommand(new string('A', 201), "Content", ValidAuthor);

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().ContainSingle(e => e.PropertyName == "Title");
	}

	[Fact]
	public void Validate_TitleAtMaxLength_ReturnsNoErrors()
	{
		// Arrange
		var command = new CreateBlogPostCommand(new string('A', 200), "Content", ValidAuthor);

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_WhitespaceTitle_ReturnsError()
	{
		// Arrange
		var command = new CreateBlogPostCommand("   ", "Content", ValidAuthor);

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Title");
	}

	[Fact]
	public void Validate_WhitespaceContent_ReturnsError()
	{
		// Arrange
		var command = new CreateBlogPostCommand("Title", "   ", ValidAuthor);

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Content");
	}
}
