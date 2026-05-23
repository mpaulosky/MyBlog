//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     GetCategoriesHandlerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using MyBlog.Domain.Abstractions;
using MyBlog.Domain.Interfaces;
using MyBlog.Web.Data;
using MyBlog.Web.Features.Categories.List;

namespace Web.Features.Categories.Handlers;

public class GetCategoriesHandlerTests
{
	private readonly ICategoryRepository _repo = Substitute.For<ICategoryRepository>();
	private readonly GetCategoriesHandler _handler;

	public GetCategoriesHandlerTests()
	{
		_handler = new GetCategoriesHandler(_repo);
	}

	[Fact]
	public async Task Handle_WithCategories_ReturnsMappedDtos()
	{
		// Arrange
		var cat1 = Category.Create("Technology", "Tech posts.");
		var cat2 = Category.Create("Design", "Design posts.");
		_repo.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(new List<Category> { cat1, cat2 });

		// Act
		var result = await _handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(2);
		result.Value!.Should().Contain(d => d.Name == "Technology");
		result.Value!.Should().Contain(d => d.Name == "Design");
	}

	[Fact]
	public async Task Handle_EmptyRepository_ReturnsEmptyList()
	{
		// Arrange
		_repo.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(new List<Category>());

		// Act
		var result = await _handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task Handle_RepoThrowsInvalidOperation_ReturnsFailResult()
	{
		// Arrange
		_repo.GetAllAsync(Arg.Any<CancellationToken>())
			.ThrowsAsync(new InvalidOperationException("db error"));

		// Act
		var result = await _handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("db error");
	}

	[Fact]
	public async Task Handle_UnexpectedException_ReturnsGenericError()
	{
		// Arrange
		_repo.GetAllAsync(Arg.Any<CancellationToken>())
			.ThrowsAsync(new TimeoutException("db timeout"));

		// Act
		var result = await _handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Be("An unexpected error occurred.");
	}

	[Fact]
	public async Task Handle_OperationCanceled_Rethrows()
	{
		// Arrange
		_repo.GetAllAsync(Arg.Any<CancellationToken>())
			.ThrowsAsync(new OperationCanceledException());

		// Act
		Func<Task> act = () => _handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

		// Assert
		await act.Should().ThrowAsync<OperationCanceledException>();
	}
}
