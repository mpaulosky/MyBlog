//Project Name :  Web.Tests
//=======================================================

using MyBlog.Web.Features.BlogPosts.Edit;

namespace Web.Features.BlogPosts.Commands;

public class EditBlogPostCommandValidatorTests
{
	private readonly EditBlogPostCommandValidator _sut = new();

	[Fact]
	public void Validate_ValidCommand_ReturnsNoErrors()
	{
		// Arrange
		var command = new EditBlogPostCommand(Guid.NewGuid(), "Valid Title", "Valid Content");

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_EmptyId_ReturnsError()
	{
		// Arrange
		var command = new EditBlogPostCommand(Guid.Empty, "Title", "Content");

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Id");
	}

	[Fact]
	public void Validate_EmptyTitle_ReturnsError()
	{
		// Arrange
		var command = new EditBlogPostCommand(Guid.NewGuid(), "", "Content");

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Title");
	}

	[Fact]
	public void Validate_EmptyContent_ReturnsError()
	{
		// Arrange
		var command = new EditBlogPostCommand(Guid.NewGuid(), "Title", "");

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Content");
	}

	[Fact]
	public void Validate_TitleExceedsMaxLength_ReturnsError()
	{
		// Arrange
		var command = new EditBlogPostCommand(Guid.NewGuid(), new string('A', 201), "Content");

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
		var command = new EditBlogPostCommand(Guid.NewGuid(), new string('A', 200), "Content");

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeTrue();
	}

	[Fact]
	public void Validate_WhitespaceTitle_ReturnsError()
	{
		// Arrange
		var command = new EditBlogPostCommand(Guid.NewGuid(), "   ", "Content");

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
		var command = new EditBlogPostCommand(Guid.NewGuid(), "Title", "   ");

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == "Content");
	}

	[Fact]
	public void Validate_MultipleEmptyFields_ReturnsMultipleErrors()
	{
		// Arrange
		var command = new EditBlogPostCommand(Guid.Empty, "", "");

		// Act
		var result = _sut.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().HaveCountGreaterThan(1);
	}
}
