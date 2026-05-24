//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     GetCategoryByIdHandlerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using MyBlog.Domain.Abstractions;
using MyBlog.Domain.Interfaces;
using MyBlog.Web.Data;
using MyBlog.Web.Features.Categories.GetById;

namespace Web.Features.Categories.Handlers;

public class GetCategoryByIdHandlerTests
{
	private readonly ICategoryRepository _repo = Substitute.For<ICategoryRepository>();
	private readonly GetCategoryByIdHandler _handler;

	public GetCategoryByIdHandlerTests()
	{
		_handler = new GetCategoryByIdHandler(_repo);
	}

	[Fact]
	public async Task Handle_CategoryExists_ReturnsMappedDto()
	{
		// Arrange
		var category = Category.Create("Technology", "Tech posts.");
		_repo.GetByIdAsync(category.Id, Arg.Any<CancellationToken>())
			.Returns(category);

		// Act
		var result = await _handler.Handle(new GetCategoryByIdQuery(category.Id), CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Name.Should().Be("Technology");
		result.Value.Description.Should().Be("Tech posts.");
		result.Value.Id.Should().Be(category.Id.ToString());
	}

	[Fact]
	public async Task Handle_CategoryNotFound_ReturnsSuccessWithNullValue()
	{
		// Arrange
		var missingId = ObjectId.GenerateNewId();
		_repo.GetByIdAsync(missingId, Arg.Any<CancellationToken>())
			.Returns((Category?)null);

		// Act
		var result = await _handler.Handle(new GetCategoryByIdQuery(missingId), CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeNull();
	}

	[Fact]
	public async Task Handle_RepoThrowsInvalidOperation_ReturnsFailResult()
	{
		// Arrange
		var id = ObjectId.GenerateNewId();
		_repo.GetByIdAsync(id, Arg.Any<CancellationToken>())
			.ThrowsAsync(new InvalidOperationException("db error"));

		// Act
		var result = await _handler.Handle(new GetCategoryByIdQuery(id), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("db error");
	}

	[Fact]
	public async Task Handle_UnexpectedException_ReturnsGenericError()
	{
		// Arrange
		var id = ObjectId.GenerateNewId();
		_repo.GetByIdAsync(id, Arg.Any<CancellationToken>())
			.ThrowsAsync(new TimeoutException("timeout"));

		// Act
		var result = await _handler.Handle(new GetCategoryByIdQuery(id), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Be("An unexpected error occurred.");
	}

	[Fact]
	public async Task Handle_OperationCanceled_Rethrows()
	{
		// Arrange
		var id = ObjectId.GenerateNewId();
		_repo.GetByIdAsync(id, Arg.Any<CancellationToken>())
			.ThrowsAsync(new OperationCanceledException());

		// Act
		Func<Task> act = () => _handler.Handle(new GetCategoryByIdQuery(id), CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<OperationCanceledException>();
	}
}
