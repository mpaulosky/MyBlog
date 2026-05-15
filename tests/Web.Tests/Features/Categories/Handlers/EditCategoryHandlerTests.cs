//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     EditCategoryHandlerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using MyBlog.Domain.Abstractions;
using MyBlog.Domain.Interfaces;
using MyBlog.Web.Features.Categories.Edit;

namespace Web.Features.Categories.Handlers;

public class EditCategoryHandlerTests
{
	private readonly ICategoryRepository _repo = Substitute.For<ICategoryRepository>();
	private readonly EditCategoryHandler _handler;

	public EditCategoryHandlerTests()
	{
		_handler = new EditCategoryHandler(_repo);
	}

	[Fact]
	public async Task Handle_ValidCommand_UpdatesAndReturnsSuccess()
	{
		// Arrange
		var category = Category.Create("Technology", "Old description.");
		_repo.GetByIdAsync(category.Id, Arg.Any<CancellationToken>())
			.Returns(category);
		_repo.ExistsByNameExcludingAsync("Technology Updated", category.Id, Arg.Any<CancellationToken>())
			.Returns(false);

		var command = new EditCategoryCommand(category.Id, "Technology Updated", "New description.");

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		await _repo.Received(1).UpdateAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_CategoryNotFound_ReturnsNotFoundFailResult()
	{
		// Arrange
		var id = Guid.NewGuid();
		_repo.GetByIdAsync(id, Arg.Any<CancellationToken>())
			.Returns((Category?)null);

		var command = new EditCategoryCommand(id, "Technology", "Description.");

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public async Task Handle_DuplicateNameOnDifferentCategory_ReturnsConflictFailResult()
	{
		// Arrange
		var category = Category.Create("Technology", "Description.");
		_repo.GetByIdAsync(category.Id, Arg.Any<CancellationToken>())
			.Returns(category);
		_repo.ExistsByNameExcludingAsync("Design", category.Id, Arg.Any<CancellationToken>())
			.Returns(true);

		var command = new EditCategoryCommand(category.Id, "Design", "Another description.");

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.Conflict);
		result.Error.Should().Contain("Design");
	}

	[Fact]
	public async Task Handle_RepoThrows_ReturnsFailResult()
	{
		// Arrange
		var category = Category.Create("Technology", "Description.");
		_repo.GetByIdAsync(category.Id, Arg.Any<CancellationToken>())
			.Returns(category);
		_repo.ExistsByNameExcludingAsync(Arg.Any<string>(), category.Id, Arg.Any<CancellationToken>())
			.Returns(false);
		_repo.UpdateAsync(Arg.Any<Category>(), Arg.Any<CancellationToken>())
			.ThrowsAsync(new InvalidOperationException("db error"));

		var command = new EditCategoryCommand(category.Id, "Technology Updated", "New description.");

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
		var id = Guid.NewGuid();
		_repo.GetByIdAsync(id, Arg.Any<CancellationToken>())
			.ThrowsAsync(new OperationCanceledException());

		var command = new EditCategoryCommand(id, "Technology", "Description.");

		// Act
		Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<OperationCanceledException>();
	}
}
