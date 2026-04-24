//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     ResultTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Unit.Tests
//=======================================================

using System.Reflection;
using MyBlog.Domain.Abstractions;

namespace Web;

public class ResultTests
{
	[Fact]
	public void OkCreatesSuccessfulNonGenericResult()
	{
		// Arrange (none)
		// Act
		var result = Result.Ok();

		result.Success.Should().BeTrue();
		result.Failure.Should().BeFalse();
		result.Error.Should().BeNull();
		result.ErrorCode.Should().Be(ResultErrorCode.None);
	}

	[Fact]
	public void FailCreatesFailedNonGenericResultWithCodeAndDetails()
	{
		// Arrange (none)
		// Act
		var result = Result.Fail("boom", ResultErrorCode.Validation, new { Field = "Title" });

		result.Success.Should().BeFalse();
		result.Failure.Should().BeTrue();
		result.Error.Should().Be("boom");
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		result.Details.Should().NotBeNull();
	}

	[Fact]
	public void GenericOkCarriesValue()
	{
		// Arrange (none)
		// Act
		var result = Result.Ok("hello");

		result.Success.Should().BeTrue();
		result.Value.Should().Be("hello");
	}

	[Fact]
	public void GenericFailCreatesFailedResultWithCode()
	{
		// Arrange (none)
		// Act
		var result = Result.Fail<string>("missing", ResultErrorCode.NotFound);

		result.Success.Should().BeFalse();
		result.Error.Should().Be("missing");
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
		result.Value.Should().BeNull();
	}

	[Fact]
	public void FromValueReturnsFailedResultWhenValueIsNull()
	{
		// Arrange
		string? value = null;

		// Act
		var result = Result.FromValue(value);

		result.Success.Should().BeFalse();
		result.Error.Should().Be("Provided value is null.");
	}

	[Fact]
	public void GenericResultUsesExplicitToValue()
	{
		// Arrange
		var result = Result<string>.FromValue("hello");

		// Act
		string? value = result.ToValue();

		// Assert
		value.Should().Be("hello");
		result.Value.Should().Be("hello");
	}

	[Fact]
	public void GenericFailRequiresExplicitToValueAndReturnsNull()
	{
		// Arrange
		var result = Result.Fail<string>("missing", ResultErrorCode.NotFound);

		// Act
		string? value = result.ToValue();

		// Assert
		value.Should().BeNull();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public void ResultTypesExposeNoConversionOperators()
	{
		// Arrange
		var operatorNames = new[] { "op_Implicit", "op_Explicit" };

		// Act
		var operatorMethods = typeof(Result)
			.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
			.Concat(typeof(Result<>).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
			.Where(method => operatorNames.Contains(method.Name))
			.Select(method => method.Name)
			.ToArray();

		// Assert
		operatorMethods.Should().BeEmpty();
	}
}
