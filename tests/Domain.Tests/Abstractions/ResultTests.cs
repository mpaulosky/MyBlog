//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     ResultTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain.Tests
//=======================================================

using System.Reflection;

namespace MyBlog.Domain.Tests.Abstractions;

public class ResultTests
{
	[Fact]
	public void OkReturnsSuccessResultWithNoError()
	{
		// Arrange / Act
		var result = Result.Ok();

		// Assert
		result.Success.Should().BeTrue();
		result.Failure.Should().BeFalse();
		result.Error.Should().BeNull();
	}

	[Fact]
	public void FailWithMessageReturnsFailureResultWithMessage()
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
	public void FailWithMessageAndErrorCodeReturnsFailureResultWithCode()
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
	public void OkTReturnsSuccessResultWithValue()
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
	public void FailTWithMessageReturnsFailureResultWithNullValue()
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
	public void FromValueNonNullValueReturnsSuccessResult()
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
	public void FromValueNullValueReturnsFailureResultWithProvidedValueIsNullError()
	{
		// Arrange / Act
		var result = Result.FromValue<string>(null);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Be("Provided value is null.");
	}

	[Fact]
	public void FromValueProducesSuccessResult()
	{
		// Arrange / Act
		var result = Result<string>.FromValue("world");

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().Be("world");
	}

	[Fact]
	public void ToValueReturnsValue()
	{
		// Arrange
		var result = Result.Ok<string>("data");

		// Act
		string? value = result.ToValue();

		// Assert
		value.Should().Be("data");
	}

	[Fact]
	public void NullResultTToValueViaConditionalAccessReturnsNull()
	{
		// Arrange
		var result = GetNullStringResult();

		// Act
		string? value = result?.ToValue();

		// Assert
		value.Should().BeNull();
	}

	private static Result<string>? GetNullStringResult() => null;

	[Fact]
	public void OkErrorCodeIsNone()
	{
		// Arrange / Act
		var result = Result.Ok();

		// Assert
		result.ErrorCode.Should().Be(ResultErrorCode.None);
	}

	[Fact]
	public void FailWithoutCodeErrorCodeIsNone()
	{
		// Arrange / Act
		var result = Result.Fail("error");

		// Assert
		result.ErrorCode.Should().Be(ResultErrorCode.None);
	}

	[Fact]
	public void GenericFromValueNullReturnsFailureResultWithValueCannotBeNullError()
	{
		// Arrange / Act
		var result = Result<string>.FromValue(null);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Be("Value cannot be null.");
		result.Value.Should().BeNull();
	}

	[Fact]
	public void GenericFailToValueReturnsNull()
	{
		// Arrange
		var result = Result.Fail<string>("bad input", ResultErrorCode.Validation);

		// Act
		string? value = result.ToValue();

		// Assert
		value.Should().BeNull();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public void ResultTypesDoNotDeclareImplicitOrExplicitConversionOperators()
	{
		// Arrange
		var operatorNames = new[] { "op_Implicit", "op_Explicit" };

		// Act
		var resultOperators = typeof(Result)
			.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
			.Where(method => operatorNames.Contains(method.Name))
			.Select(method => method.Name)
			.ToArray();

		var genericResultOperators = typeof(Result<>)
			.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
			.Where(method => operatorNames.Contains(method.Name))
			.Select(method => method.Name)
			.ToArray();

		// Assert
		resultOperators.Should().BeEmpty();
		genericResultOperators.Should().BeEmpty();
	}
}
