//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     ResultTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using MyBlog.Domain.Abstractions;

namespace Web;

public class ResultTests
{
	[Fact]
	public void Ok_CreatesSuccessfulNonGenericResult()
	{
		// Arrange (none)

		// Act
		var result = Result.Ok();

		// Assert
		result.Success.Should().BeTrue();
		result.Failure.Should().BeFalse();
		result.Error.Should().BeNull();
		result.ErrorCode.Should().Be(ResultErrorCode.None);
	}

	[Fact]
	public void Fail_CreatesFailedNonGenericResultWithCodeAndDetails()
	{
		// Arrange (none)

		// Act
		var result = Result.Fail("boom", ResultErrorCode.Validation, new { Field = "Title" });

		// Assert
		result.Success.Should().BeFalse();
		result.Failure.Should().BeTrue();
		result.Error.Should().Be("boom");
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		result.Details.Should().NotBeNull();
	}

	[Fact]
	public void GenericOk_CarriesValue()
	{
		// Arrange (none)

		// Act
		var result = Result.Ok("hello");

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().Be("hello");
	}

	[Fact]
	public void GenericFail_CreatesFailedResultWithCode()
	{
		// Arrange (none)

		// Act
		var result = Result.Fail<string>("missing", ResultErrorCode.NotFound);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("missing");
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
		result.Value.Should().BeNull();
	}

	[Fact]
	public void FromValue_ReturnsFailedResultWhenValueIsNull()
	{
		// Arrange
		string? value = null;

		// Act
		var result = Result.FromValue(value);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("Provided value is null.");
	}

	[Fact]
	public void ImplicitConversions_WorkForGenericResult()
	{
		// Arrange
		Result<string> result = "hello";

		// Act
		string? value = result;

		// Assert
		value.Should().Be("hello");
		result.Value.Should().Be("hello");
	}
}
