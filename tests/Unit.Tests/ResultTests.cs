using Domain.Abstractions;
using FluentAssertions;

namespace MyBlog.Unit.Tests;

public class ResultTests
{
    [Fact]
    public void Ok_CreatesSuccessfulNonGenericResult()
    {
        var result = Result.Ok();

        result.Success.Should().BeTrue();
        result.Failure.Should().BeFalse();
        result.Error.Should().BeNull();
        result.ErrorCode.Should().Be(ResultErrorCode.None);
    }

    [Fact]
    public void Fail_CreatesFailedNonGenericResultWithCodeAndDetails()
    {
        var result = Result.Fail("boom", ResultErrorCode.Validation, new { Field = "Title" });

        result.Success.Should().BeFalse();
        result.Failure.Should().BeTrue();
        result.Error.Should().Be("boom");
        result.ErrorCode.Should().Be(ResultErrorCode.Validation);
        result.Details.Should().NotBeNull();
    }

    [Fact]
    public void GenericOk_CarriesValue()
    {
        var result = Result.Ok("hello");

        result.Success.Should().BeTrue();
        result.Value.Should().Be("hello");
    }

    [Fact]
    public void GenericFail_CreatesFailedResultWithCode()
    {
        var result = Result.Fail<string>("missing", ResultErrorCode.NotFound);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("missing");
        result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
        result.Value.Should().BeNull();
    }

    [Fact]
    public void FromValue_ReturnsFailedResultWhenValueIsNull()
    {
        string? value = null;

        var result = Result.FromValue(value);

        result.Success.Should().BeFalse();
        result.Error.Should().Be("Provided value is null.");
    }

    [Fact]
    public void ImplicitConversions_WorkForGenericResult()
    {
        Result<string> result = "hello";
        string? value = result;

        value.Should().Be("hello");
        result.Value.Should().Be("hello");
    }
}
