//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     ValidationBehaviorTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Unit.Tests
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
	// ── CreateBlogPostCommand (Result<Guid>) ─────────────────────────────────

	[Fact]
	public async Task Handle_NoValidators_CallsNext()
	{
		var next = Substitute.For<RequestHandlerDelegate<Result<Guid>>>();
		next(Arg.Any<CancellationToken>()).Returns(Result.Ok<Guid>(Guid.NewGuid()));
		var behavior = new ValidationBehavior<CreateBlogPostCommand, Result<Guid>>([]);

		var result = await behavior.Handle(
			new CreateBlogPostCommand("T", "C", "A"), next, CancellationToken.None);

		result.Success.Should().BeTrue();
		await next.Received(1)(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_ValidRequest_CallsNext()
	{
		var validator = new CreateBlogPostCommandValidator();
		var next = Substitute.For<RequestHandlerDelegate<Result<Guid>>>();
		next(Arg.Any<CancellationToken>()).Returns(Result.Ok<Guid>(Guid.NewGuid()));
		var behavior = new ValidationBehavior<CreateBlogPostCommand, Result<Guid>>([validator]);

		var result = await behavior.Handle(
			new CreateBlogPostCommand("Title", "Content", "Author"), next, CancellationToken.None);

		result.Success.Should().BeTrue();
		await next.Received(1)(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_InvalidRequest_ReturnsValidationFailWithoutCallingNext()
	{
		var validator = new CreateBlogPostCommandValidator();
		var next = Substitute.For<RequestHandlerDelegate<Result<Guid>>>();
		var behavior = new ValidationBehavior<CreateBlogPostCommand, Result<Guid>>([validator]);

		var result = await behavior.Handle(
			new CreateBlogPostCommand("", "", ""), next, CancellationToken.None);

		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		await next.DidNotReceive()(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_InvalidRequest_ErrorMessageContainsValidationDetails()
	{
		var validator = new CreateBlogPostCommandValidator();
		var next = Substitute.For<RequestHandlerDelegate<Result<Guid>>>();
		var behavior = new ValidationBehavior<CreateBlogPostCommand, Result<Guid>>([validator]);

		var result = await behavior.Handle(
			new CreateBlogPostCommand("", "Content", ""), next, CancellationToken.None);

		result.Error.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public async Task Handle_MultipleValidators_AllAreExecuted()
	{
		var validator1 = new CreateBlogPostCommandValidator();
		var validator2 = new CreateBlogPostCommandValidator();
		var next = Substitute.For<RequestHandlerDelegate<Result<Guid>>>();
		next(Arg.Any<CancellationToken>()).Returns(Result.Ok<Guid>(Guid.NewGuid()));
		var behavior = new ValidationBehavior<CreateBlogPostCommand, Result<Guid>>([validator1, validator2]);

		var result = await behavior.Handle(
			new CreateBlogPostCommand("Title", "Content", "Author"), next, CancellationToken.None);

		result.Success.Should().BeTrue();
		await next.Received(1)(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_MultipleValidatorsOneInvalid_ReturnsFail()
	{
		var validator1 = new CreateBlogPostCommandValidator();
		var validator2 = new CreateBlogPostCommandValidator();
		var next = Substitute.For<RequestHandlerDelegate<Result<Guid>>>();
		var behavior = new ValidationBehavior<CreateBlogPostCommand, Result<Guid>>([validator1, validator2]);

		var result = await behavior.Handle(
			new CreateBlogPostCommand("", "", ""), next, CancellationToken.None);

		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		await next.DidNotReceive()(Arg.Any<CancellationToken>());
	}

	// ── DeleteBlogPostCommand (Result — non-generic) ─────────────────────────

	[Fact]
	public async Task Handle_DeleteNoValidators_CallsNext()
	{
		var next = Substitute.For<RequestHandlerDelegate<Result>>();
		next(Arg.Any<CancellationToken>()).Returns(Result.Ok());
		var behavior = new ValidationBehavior<DeleteBlogPostCommand, Result>([]);

		var result = await behavior.Handle(
			new DeleteBlogPostCommand(Guid.NewGuid()), next, CancellationToken.None);

		result.Success.Should().BeTrue();
		await next.Received(1)(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_DeleteValidRequest_CallsNext()
	{
		var validator = new DeleteBlogPostCommandValidator();
		var next = Substitute.For<RequestHandlerDelegate<Result>>();
		next(Arg.Any<CancellationToken>()).Returns(Result.Ok());
		var behavior = new ValidationBehavior<DeleteBlogPostCommand, Result>([validator]);

		var result = await behavior.Handle(
			new DeleteBlogPostCommand(Guid.NewGuid()), next, CancellationToken.None);

		result.Success.Should().BeTrue();
		await next.Received(1)(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_DeleteEmptyGuid_ReturnsValidationFailWithoutCallingNext()
	{
		var validator = new DeleteBlogPostCommandValidator();
		var next = Substitute.For<RequestHandlerDelegate<Result>>();
		var behavior = new ValidationBehavior<DeleteBlogPostCommand, Result>([validator]);

		var result = await behavior.Handle(
			new DeleteBlogPostCommand(Guid.Empty), next, CancellationToken.None);

		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		await next.DidNotReceive()(Arg.Any<CancellationToken>());
	}

	// ── EditBlogPostCommand (Result — non-generic) ────────────────────────────

	[Fact]
	public async Task Handle_EditValidRequest_CallsNext()
	{
		var validator = new EditBlogPostCommandValidator();
		var next = Substitute.For<RequestHandlerDelegate<Result>>();
		next(Arg.Any<CancellationToken>()).Returns(Result.Ok());
		var behavior = new ValidationBehavior<EditBlogPostCommand, Result>([validator]);

		var result = await behavior.Handle(
			new EditBlogPostCommand(Guid.NewGuid(), "Title", "Content"), next, CancellationToken.None);

		result.Success.Should().BeTrue();
		await next.Received(1)(Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_EditInvalidRequest_ReturnsValidationFailWithoutCallingNext()
	{
		var validator = new EditBlogPostCommandValidator();
		var next = Substitute.For<RequestHandlerDelegate<Result>>();
		var behavior = new ValidationBehavior<EditBlogPostCommand, Result>([validator]);

		var result = await behavior.Handle(
			new EditBlogPostCommand(Guid.Empty, "", ""), next, CancellationToken.None);

		result.Success.Should().BeFalse();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		await next.DidNotReceive()(Arg.Any<CancellationToken>());
	}
}
