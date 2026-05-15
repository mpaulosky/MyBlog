//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CreateCategoryHandlerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

// Staged #339 — awaiting CreateCategoryHandler from Sam.
// Remove [Skip] attributes and uncomment handler construction once the handler lands.

namespace Web.Features.Categories.Handlers;

public class CreateCategoryHandlerTests
{
	// ── Staged: CreateCategoryHandler ────────────────────────────────────

	[Fact(Skip = "Staged #339: awaiting CreateCategoryHandler")]
	public async Task Handle_ValidCommand_PersistsAndReturnsNewId()
	{
		// Will verify: CreateCategoryCommand("Tech", "Description.") → Success, Value is non-empty Guid
		await Task.CompletedTask;
	}

	[Fact(Skip = "Staged #339: awaiting CreateCategoryHandler")]
	public async Task Handle_DuplicateName_ReturnsConflictFailResult()
	{
		// Will verify: when ICategoryRepository.ExistsByNameAsync returns true
		// → Result.Failure with ResultErrorCode.Conflict
		await Task.CompletedTask;
	}

	[Fact(Skip = "Staged #339: awaiting CreateCategoryHandler")]
	public async Task Handle_RepoThrows_ReturnsFailResult()
	{
		// Will verify: repo.AddAsync throws → handler catches, returns failure
		await Task.CompletedTask;
	}

	[Fact(Skip = "Staged #339: awaiting CreateCategoryHandler")]
	public async Task Handle_OperationCanceled_Rethrows()
	{
		// Will verify: OperationCanceledException propagates
		await Task.CompletedTask;
	}
}
