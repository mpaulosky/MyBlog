//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     DeleteCategoryHandlerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using MyBlog.Domain.Abstractions;
using MyBlog.Domain.Interfaces;
using MyBlog.Web.Features.Categories.Delete;

namespace Web.Features.Categories.Handlers;

public class DeleteCategoryHandlerTests
{
	private readonly ICategoryRepository _categoryRepo = Substitute.For<ICategoryRepository>();
	private readonly IBlogPostRepository _blogPostRepo = Substitute.For<IBlogPostRepository>();
	private readonly DeleteCategoryHandler _handler;

	public DeleteCategoryHandlerTests()
	{
		_handler = new DeleteCategoryHandler(_categoryRepo, _blogPostRepo);
	}

	[Fact]
	public async Task Handle_CategoryWithNoPosts_DeletesAndReturnsSuccess()
	{
		// Arrange
		var category = Category.Create("Technology", "All about tech.");
		_categoryRepo.GetByIdAsync(category.Id, Arg.Any<CancellationToken>())
			.Returns(category);
		_blogPostRepo.ExistsByCategoryAsync(category.Id, Arg.Any<CancellationToken>())
			.Returns(false);

		var command = new DeleteCategoryCommand(category.Id);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		await _categoryRepo.Received(1).DeleteAsync(category.Id, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_CategoryInUseByPosts_ReturnsConflictFailResult()
	{
		// AC: "cannot delete category in use"
		// Arrange
		var category = Category.Create("Technology", "All about tech.");
		_categoryRepo.GetByIdAsync(category.Id, Arg.Any<CancellationToken>())
			.Returns(category);
		_blogPostRepo.ExistsByCategoryAsync(category.Id, Arg.Any<CancellationToken>())
			.Returns(true);

		var command = new DeleteCategoryCommand(category.Id);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.Conflict);
		result.Error.Should().Contain("Technology");
		await _categoryRepo.DidNotReceive().DeleteAsync(Arg.Any<ObjectId>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Handle_CategoryNotFound_ReturnsNotFoundFailResult()
	{
		// Arrange
		var id = ObjectId.GenerateNewId();
		_categoryRepo.GetByIdAsync(id, Arg.Any<CancellationToken>())
			.Returns((Category?)null);

		var command = new DeleteCategoryCommand(id);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
	}

	[Fact]
	public async Task Handle_RepoThrows_ReturnsFailResult()
	{
		// Arrange
		var category = Category.Create("Technology", "All about tech.");
		_categoryRepo.GetByIdAsync(category.Id, Arg.Any<CancellationToken>())
			.Returns(category);
		_blogPostRepo.ExistsByCategoryAsync(category.Id, Arg.Any<CancellationToken>())
			.Returns(false);
		_categoryRepo.DeleteAsync(category.Id, Arg.Any<CancellationToken>())
			.ThrowsAsync(new InvalidOperationException("db error"));

		var command = new DeleteCategoryCommand(category.Id);

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
		var id = ObjectId.GenerateNewId();
		_categoryRepo.GetByIdAsync(id, Arg.Any<CancellationToken>())
			.ThrowsAsync(new OperationCanceledException());

		var command = new DeleteCategoryCommand(id);

		// Act
		Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<OperationCanceledException>();
	}
}
