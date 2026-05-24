//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     ValidationBehaviorTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using MediatR;

using MyBlog.Domain.Abstractions;
using MyBlog.Domain.Behaviors;
using MyBlog.Web.Features.BlogPosts.Create;
using MyBlog.Web.Features.BlogPosts.Delete;
using MyBlog.Web.Features.BlogPosts.Edit;

namespace Web.Behaviors;

public class ValidationBehaviorTests
{
	// ── CreateBlogPostCommand (Result<ObjectId>) ─────────────────────────────────

	[Fact]
	public async Task Handle_NoValidators_CallsNext()
	{
		// Arrange
		var next = Substitute.For<RequestHandlerDelegate<Result<ObjectId>>>();
		next(Arg.Any<CancellationToken>()).Returns(Result.Ok<ObjectId>(ObjectId.GenerateNewId()));
		var behavior = new ValidationBehavior<CreateBlogPostCommand, Result<ObjectId>>([]);

		// Act
		var result = await behavior.Handle(
			new CreateBlogPostCommand("T", "C", PostAuthor.Empty), next, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		await next.Received(1)(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_ValidRequest_CallsNext()
	{
		// Arrange
		var validator = new CreateBlogPostCommandValidator();
		var next = Substitute.For<RequestHandlerDelegate<Result<ObjectId>>>();
		next(Arg.Any<CancellationToken>()).Returns(Result.Ok<ObjectId>(ObjectId.GenerateNewId()));
		var behavior = new ValidationBehavior<CreateBlogPostCommand, Result<ObjectId>>([validator]);

		// Act
		var result = await behavior.Handle(
			new CreateBlogPostCommand("Title", "Content", new PostAuthor("", "Author", "", [])), next, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		await next.Received(1)(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_InvalidRequest_ReturnsValidationFailWithoutCallingNext()
	{
		// Arrange
		var validator = new CreateBlogPostCommandValidator();
		var next = Substitute.For<RequestHandlerDelegate<Result<ObjectId>>>();
		var behavior = new ValidationBehavior<CreateBlogPostCommand, Result<ObjectId>>([validator]);

		// Act
		var result = await behavior.Handle(
			new CreateBlogPostCommand("", "", PostAuthor.Empty), next, CancellationToken.None);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		await next.DidNotReceive()(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_InvalidRequest_ErrorMessageContainsValidationDetails()
	{
		// Arrange
		var validator = new CreateBlogPostCommandValidator();
		var next = Substitute.For<RequestHandlerDelegate<Result<ObjectId>>>();
		var behavior = new ValidationBehavior<CreateBlogPostCommand, Result<ObjectId>>([validator]);

		// Act
		var result = await behavior.Handle(
			new CreateBlogPostCommand("", "Content", PostAuthor.Empty), next, CancellationToken.None);

		// Assert
		result.Error.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public async Task Handle_MultipleValidators_AllAreExecuted()
	{
		// Arrange
		var validator1 = new CreateBlogPostCommandValidator();
		var validator2 = new CreateBlogPostCommandValidator();
		var next = Substitute.For<RequestHandlerDelegate<Result<ObjectId>>>();
		next(Arg.Any<CancellationToken>()).Returns(Result.Ok<ObjectId>(ObjectId.GenerateNewId()));
		var behavior = new ValidationBehavior<CreateBlogPostCommand, Result<ObjectId>>([validator1, validator2]);

		// Act
		var result = await behavior.Handle(
			new CreateBlogPostCommand("Title", "Content", new PostAuthor("", "Author", "", [])), next, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		await next.Received(1)(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_MultipleValidatorsOneInvalid_ReturnsFail()
	{
		// Arrange
		var validator1 = new CreateBlogPostCommandValidator();
		var validator2 = new CreateBlogPostCommandValidator();
		var next = Substitute.For<RequestHandlerDelegate<Result<ObjectId>>>();
		var behavior = new ValidationBehavior<CreateBlogPostCommand, Result<ObjectId>>([validator1, validator2]);

		// Act
		var result = await behavior.Handle(
			new CreateBlogPostCommand("", "", PostAuthor.Empty), next, CancellationToken.None);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		await next.DidNotReceive()(Arg.Any<CancellationToken>());
	}

	// ── DeleteBlogPostCommand (Result — non-generic) ─────────────────────────

	[Fact]
	public async Task Handle_DeleteNoValidators_CallsNext()
	{
		// Arrange
		var next = Substitute.For<RequestHandlerDelegate<Result>>();
		next(Arg.Any<CancellationToken>()).Returns(Result.Ok());
		var behavior = new ValidationBehavior<DeleteBlogPostCommand, Result>([]);

		// Act
		var result = await behavior.Handle(
			new DeleteBlogPostCommand(ObjectId.GenerateNewId()), next, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		await next.Received(1)(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_DeleteValidRequest_CallsNext()
	{
		// Arrange
		var validator = new DeleteBlogPostCommandValidator();
		var next = Substitute.For<RequestHandlerDelegate<Result>>();
		next(Arg.Any<CancellationToken>()).Returns(Result.Ok());
		var behavior = new ValidationBehavior<DeleteBlogPostCommand, Result>([validator]);

		// Act
		var result = await behavior.Handle(
			new DeleteBlogPostCommand(ObjectId.GenerateNewId()), next, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		await next.Received(1)(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_DeleteEmptyObjectId_ReturnsValidationFailWithoutCallingNext()
	{
		// Arrange
		var validator = new DeleteBlogPostCommandValidator();
		var next = Substitute.For<RequestHandlerDelegate<Result>>();
		var behavior = new ValidationBehavior<DeleteBlogPostCommand, Result>([validator]);

		// Act
		var result = await behavior.Handle(
			new DeleteBlogPostCommand(ObjectId.Empty), next, CancellationToken.None);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		await next.DidNotReceive()(Arg.Any<CancellationToken>());
	}

	// ── EditBlogPostCommand (Result — non-generic) ────────────────────────────

	[Fact]
	public async Task Handle_EditValidRequest_CallsNext()
	{
		// Arrange
		var validator = new EditBlogPostCommandValidator();
		var next = Substitute.For<RequestHandlerDelegate<Result>>();
		next(Arg.Any<CancellationToken>()).Returns(Result.Ok());
		var behavior = new ValidationBehavior<EditBlogPostCommand, Result>([validator]);

		// Act
		var result = await behavior.Handle(
			new EditBlogPostCommand(ObjectId.GenerateNewId(), "Title", "Content", string.Empty, false), next, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		await next.Received(1)(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_EditInvalidRequest_ReturnsValidationFailWithoutCallingNext()
	{
		// Arrange
		var validator = new EditBlogPostCommandValidator();
		var next = Substitute.For<RequestHandlerDelegate<Result>>();
		var behavior = new ValidationBehavior<EditBlogPostCommand, Result>([validator]);

		// Act
		var result = await behavior.Handle(
			new EditBlogPostCommand(ObjectId.Empty, "", "", string.Empty, false), next, CancellationToken.None);

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		await next.DidNotReceive()(Arg.Any<CancellationToken>());
	}
}
