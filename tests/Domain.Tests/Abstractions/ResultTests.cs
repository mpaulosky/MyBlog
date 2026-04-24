//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     ResultTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain.Tests
//=======================================================

namespace Tests.Domain.Abstractions;

public class ResultTests
{
	[Fact]
	public void Ok_ReturnsSuccessResultWithNoError()
	{
		// Arrange / Act
		var result = Result.Ok();

		// Assert
		result.Success.Should().BeTrue();
		result.Failure.Should().BeFalse();
		result.Error.Should().BeNull();
	}

	[Fact]
	public void Fail_WithMessage_ReturnsFailureResultWithMessage()
	{
		// Arrange
		const string errorMessage = "Something went wrong";

		// Act
		var result = Result.Fail(errorMessage);

		// Assert
		result.Success.Should().BeFalse();
		result.Failure.Should().BeTrue();
		result.Error.Should().Be(errorMessage);
	}

	[Fact]
	public void Fail_WithMessageAndErrorCode_ReturnsFailureResultWithCode()
	{
		// Arrange
		const string errorMessage = "Not found";

		// Act
		var result = Result.Fail(errorMessage, ResultErrorCode.NotFound);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Be(errorMessage);
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public void OkT_ReturnsSuccessResultWithValue()
	{
		// Arrange
		const string value = "hello";

		// Act
		var result = Result.Ok<string>(value);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().Be(value);
		result.Error.Should().BeNull();
	}

	[Fact]
	public void FailT_WithMessage_ReturnsFailureResultWithNullValue()
	{
		// Arrange
		const string errorMessage = "bad input";

		// Act
		var result = Result.Fail<string>(errorMessage);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Be(errorMessage);
		result.Value.Should().BeNull();
	}

	[Fact]
	public void FromValue_NonNullValue_ReturnsSuccessResult()
	{
		// Arrange
		const int value = 42;

		// Act
		var result = Result.FromValue<int?>(value);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().Be(value);
	}

	[Fact]
	public void FromValue_NullValue_ReturnsFailureResultWithProvidedValueIsNullError()
	{
		// Arrange / Act
		var result = Result.FromValue<string>(null);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Be("Provided value is null.");
	}

	[Fact]
	public void ImplicitConversion_ValueToResultT_ProducesSuccessResult()
	{
		// Arrange / Act
		Result<string> result = "world";

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().Be("world");
	}

	[Fact]
	public void ImplicitConversion_ResultTToValue_ReturnsValue()
	{
		// Arrange
		var result = Result.Ok<string>("data");

		// Act
		string? value = result;

		// Assert
		value.Should().Be("data");
	}

	[Fact]
	public void ImplicitConversion_NullResultTToValue_ReturnsNull()
	{
		// Arrange
		Result<string>? result = null;

		// Act
		string? value = result;

		// Assert
		value.Should().BeNull();
	}

	[Fact]
	public void Ok_ErrorCode_IsNone()
	{
		// Arrange / Act
		var result = Result.Ok();

		// Assert
		result.ErrorCode.Should().Be(ResultErrorCode.None);
	}

	[Fact]
	public void Fail_WithoutCode_ErrorCode_IsNone()
	{
		// Arrange / Act
		var result = Result.Fail("error");

		// Assert
		result.ErrorCode.Should().Be(ResultErrorCode.None);
	}
}
