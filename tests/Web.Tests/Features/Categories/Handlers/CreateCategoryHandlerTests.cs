//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CreateCategoryHandlerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using MyBlog.Domain.Abstractions;
using MyBlog.Domain.Interfaces;
using MyBlog.Web.Features.Categories.Create;

namespace Web.Features.Categories.Handlers;

public class CreateCategoryHandlerTests
{
	private readonly ICategoryRepository _repo = Substitute.For<ICategoryRepository>();
	private readonly CreateCategoryHandler _handler;

	public CreateCategoryHandlerTests()
	{
		_handler = new CreateCategoryHandler(_repo);
	}

	[Fact]
	public async Task Handle_ValidCommand_PersistsAndReturnsNewId()
	{
		// Arrange
		_repo.ExistsByNameAsync("Technology", Arg.Any<CancellationToken>())
			.Returns(false);

		var command = new CreateCategoryCommand("Technology", "All about tech.");

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBe(ObjectId.Empty);
		await _repo.Received(1).AddAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_DuplicateName_ReturnsConflictFailResult()
	{
		// Arrange
		_repo.ExistsByNameAsync("Technology", Arg.Any<CancellationToken>())
			.Returns(true);

		var command = new CreateCategoryCommand("Technology", "All about tech.");

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.Conflict);
		result.Error.Should().Contain("Technology");
	}

	[Fact]
	public async Task Handle_RepoThrows_ReturnsFailResult()
	{
		// Arrange
		_repo.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(false);
		_repo.AddAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>())
			.ThrowsAsync(new InvalidOperationException("db error"));

		var command = new CreateCategoryCommand("Technology", "All about tech.");

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("db error");
	}

	[Fact]
	public async Task Handle_OperationCanceled_Rethrows()
	{
		// Arrange
		_repo.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
			.ThrowsAsync(new OperationCanceledException());

		var command = new CreateCategoryCommand("Technology", "All about tech.");

		// Act
		Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<OperationCanceledException>();
	}
}
