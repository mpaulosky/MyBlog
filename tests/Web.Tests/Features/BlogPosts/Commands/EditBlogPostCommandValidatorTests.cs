//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     EditBlogPostCommandValidatorTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Unit.Tests
//=======================================================

using MyBlog.Web.Features.BlogPosts.Edit;

namespace Web.Features.BlogPosts.Commands;

public class EditBlogPostCommandValidatorTests
{
	private readonly EditBlogPostCommandValidator _sut = new();

	[Fact]
	public void Validate_ValidCommand_ReturnsNoErrors()
	{
		var command = new EditBlogPostCommand(Guid.NewGuid(), "Valid Title", "Valid Content");
		var result = _sut.Validate(command);
		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_EmptyId_ReturnsError()
	{
		var command = new EditBlogPostCommand(Guid.Empty, "Title", "Content");
		var result = _sut.Validate(command);
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Id");
	}

	[Fact]
	public void Validate_EmptyTitle_ReturnsError()
	{
		var command = new EditBlogPostCommand(Guid.NewGuid(), "", "Content");
		var result = _sut.Validate(command);
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Title");
	}

	[Fact]
	public void Validate_EmptyContent_ReturnsError()
	{
		var command = new EditBlogPostCommand(Guid.NewGuid(), "Title", "");
		var result = _sut.Validate(command);
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Content");
	}

	[Fact]
	public void Validate_TitleExceedsMaxLength_ReturnsError()
	{
		var command = new EditBlogPostCommand(Guid.NewGuid(), new string('A', 201), "Content");
		var result = _sut.Validate(command);
		result.IsValid.Should().BeFalse();
		result.Errors.Should().ContainSingle(e => e.PropertyName == "Title");
	}

	[Fact]
	public void Validate_TitleAtMaxLength_ReturnsNoErrors()
	{
		var command = new EditBlogPostCommand(Guid.NewGuid(), new string('A', 200), "Content");
		var result = _sut.Validate(command);
		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_WhitespaceTitle_ReturnsError()
	{
		var command = new EditBlogPostCommand(Guid.NewGuid(), "   ", "Content");
		var result = _sut.Validate(command);
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Title");
	}

	[Fact]
	public void Validate_WhitespaceContent_ReturnsError()
	{
		var command = new EditBlogPostCommand(Guid.NewGuid(), "Title", "   ");
		var result = _sut.Validate(command);
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Content");
	}

	[Fact]
	public void Validate_MultipleEmptyFields_ReturnsMultipleErrors()
	{
		var command = new EditBlogPostCommand(Guid.Empty, "", "");
		var result = _sut.Validate(command);
		result.IsValid.Should().BeFalse();
		result.Errors.Should().HaveCountGreaterThan(1);
	}
}
