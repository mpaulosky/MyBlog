//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     ValidationBehaviorTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain.Tests
//=======================================================

namespace Tests.Domain.Behaviors;

/// <summary>Test request returning a plain Result.</summary>
public sealed record TestRequest : IRequest<Result>;

/// <summary>Test request returning a Result with a typed value.</summary>
public sealed record TestRequestT : IRequest<Result<string>>;

public class ValidationBehaviorTests
{
	[Fact]
	public async Task Handle_NoValidators_CallsNextAndReturnsItsResult()
	{
		// Arrange
		var behavior = new ValidationBehavior<TestRequest, Result>([]);
		var expected = Result.Ok();
		RequestHandlerDelegate<Result> next = _ => Task.FromResult(expected);

		// Act
		var result = await behavior.Handle(new TestRequest(), next, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
	}

	[Fact]
	public async Task Handle_PassingValidator_CallsNextAndReturnsItsResult()
	{
		// Arrange
		var validator = Substitute.For<IValidator<TestRequest>>();
		validator.Validate(Arg.Any<ValidationContext<TestRequest>>())
			.Returns(new ValidationResult());

		var behavior = new ValidationBehavior<TestRequest, Result>([validator]);
		var expected = Result.Ok();
		RequestHandlerDelegate<Result> next = _ => Task.FromResult(expected);

		// Act
		var result = await behavior.Handle(new TestRequest(), next, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
	}

	[Fact]
	public async Task Handle_FailingValidator_ReturnsFailResultWithErrorMessage()
	{
		// Arrange
		const string errorMessage = "Title is required";
		var failure = new ValidationFailure("Title", errorMessage);

		var validator = Substitute.For<IValidator<TestRequest>>();
		validator.Validate(Arg.Any<ValidationContext<TestRequest>>())
			.Returns(new ValidationResult([failure]));

		var behavior = new ValidationBehavior<TestRequest, Result>([validator]);
		RequestHandlerDelegate<Result> next = _ => Task.FromResult(Result.Ok());

		// Act
		var result = await behavior.Handle(new TestRequest(), next, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain(errorMessage);
	}

	[Fact]
	public async Task Handle_FailingValidator_ErrorCodeIsValidation()
	{
		// Arrange
		var failure = new ValidationFailure("Content", "Content is required");

		var validator = Substitute.For<IValidator<TestRequest>>();
		validator.Validate(Arg.Any<ValidationContext<TestRequest>>())
			.Returns(new ValidationResult([failure]));

		var behavior = new ValidationBehavior<TestRequest, Result>([validator]);
		RequestHandlerDelegate<Result> next = _ => Task.FromResult(Result.Ok());

		// Act
		var result = await behavior.Handle(new TestRequest(), next, CancellationToken.None);

		// Assert
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
	}

	[Fact]
	public async Task Handle_MultipleFailingValidators_JoinsAllErrorMessages()
	{
		// Arrange
		var validator1 = Substitute.For<IValidator<TestRequest>>();
		validator1.Validate(Arg.Any<ValidationContext<TestRequest>>())
			.Returns(new ValidationResult([new ValidationFailure("Title", "Title required")]));

		var validator2 = Substitute.For<IValidator<TestRequest>>();
		validator2.Validate(Arg.Any<ValidationContext<TestRequest>>())
			.Returns(new ValidationResult([new ValidationFailure("Content", "Content required")]));

		var behavior = new ValidationBehavior<TestRequest, Result>([validator1, validator2]);
		RequestHandlerDelegate<Result> next = _ => Task.FromResult(Result.Ok());

		// Act
		var result = await behavior.Handle(new TestRequest(), next, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Title required");
		result.Error.Should().Contain("Content required");
	}

	[Fact]
	public async Task Handle_NoValidators_ResultT_CallsNextAndReturnsItsResult()
	{
		// Arrange
		var behavior = new ValidationBehavior<TestRequestT, Result<string>>([]);
		var expected = Result.Ok("response");
		RequestHandlerDelegate<Result<string>> next = _ => Task.FromResult(expected);

		// Act
		var result = await behavior.Handle(new TestRequestT(), next, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().Be("response");
	}

	[Fact]
	public async Task Handle_FailingValidator_ResultT_ReturnsFailResultWithErrorMessage()
	{
		// Arrange
		const string errorMessage = "Author is required";
		var failure = new ValidationFailure("Author", errorMessage);

		var validator = Substitute.For<IValidator<TestRequestT>>();
		validator.Validate(Arg.Any<ValidationContext<TestRequestT>>())
			.Returns(new ValidationResult([failure]));

		var behavior = new ValidationBehavior<TestRequestT, Result<string>>([validator]);
		RequestHandlerDelegate<Result<string>> next = _ => Task.FromResult(Result.Ok("ok"));

		// Act
		var result = await behavior.Handle(new TestRequestT(), next, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain(errorMessage);
		result.Value.Should().BeNull();
	}

	[Fact]
	public async Task Handle_PassingValidator_NextIsCalledExactlyOnce()
	{
		// Arrange
		var validator = Substitute.For<IValidator<TestRequest>>();
		validator.Validate(Arg.Any<ValidationContext<TestRequest>>())
			.Returns(new ValidationResult());

		var behavior = new ValidationBehavior<TestRequest, Result>([validator]);
		var callCount = 0;
		RequestHandlerDelegate<Result> next = _ =>
		{
			callCount++;
			return Task.FromResult(Result.Ok());
		};

		// Act
		await behavior.Handle(new TestRequest(), next, CancellationToken.None);

		// Assert
		callCount.Should().Be(1);
	}

	[Fact]
	public async Task Handle_FailingValidator_NextIsNeverCalled()
	{
		// Arrange
		var failure = new ValidationFailure("Title", "required");

		var validator = Substitute.For<IValidator<TestRequest>>();
		validator.Validate(Arg.Any<ValidationContext<TestRequest>>())
			.Returns(new ValidationResult([failure]));

		var behavior = new ValidationBehavior<TestRequest, Result>([validator]);
		var nextCalled = false;
		RequestHandlerDelegate<Result> next = _ =>
		{
			nextCalled = true;
			return Task.FromResult(Result.Ok());
		};

		// Act
		await behavior.Handle(new TestRequest(), next, CancellationToken.None);

		// Assert
		nextCalled.Should().BeFalse();
	}
}
