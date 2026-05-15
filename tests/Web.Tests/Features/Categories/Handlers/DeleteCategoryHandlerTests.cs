//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     DeleteCategoryHandlerTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

// Staged #339 — awaiting DeleteCategoryHandler from Sam.
// This handler MUST check IBlogPostRepository.ExistsByCategoryAsync before deleting
// and return ResultErrorCode.Conflict when posts are still assigned to the category.

namespace Web.Features.Categories.Handlers;

public class DeleteCategoryHandlerTests
{
	// ── Staged: DeleteCategoryHandler ─────────────────────────────────────

	[Fact(Skip = "Staged #339: awaiting DeleteCategoryHandler")]
	public async Task Handle_CategoryWithNoPosts_DeletesAndReturnsSuccess()
	{
		// Will verify: IBlogPostRepository.ExistsByCategoryAsync returns false
		// → repo.DeleteAsync called, result.Success == true
		await Task.CompletedTask;
	}

	[Fact(Skip = "Staged #339: awaiting DeleteCategoryHandler")]
	public async Task Handle_CategoryInUseByPosts_ReturnsConflictFailResult()
	{
		// AC: "cannot delete category in use"
		// Will verify: IBlogPostRepository.ExistsByCategoryAsync returns true
		// → result.Failure == true, ResultErrorCode.Conflict
		await Task.CompletedTask;
	}

	[Fact(Skip = "Staged #339: awaiting DeleteCategoryHandler")]
	public async Task Handle_CategoryNotFound_ReturnsNotFoundFailResult()
	{
		// Will verify: repo.GetByIdAsync returns null → Result.Fail with NotFound code
		await Task.CompletedTask;
	}

	[Fact(Skip = "Staged #339: awaiting DeleteCategoryHandler")]
	public async Task Handle_RepoThrows_ReturnsFailResult()
	{
		// Will verify: unexpected exception → Result.Fail("An unexpected error occurred.")
		await Task.CompletedTask;
	}

	[Fact(Skip = "Staged #339: awaiting DeleteCategoryHandler")]
	public async Task Handle_OperationCanceled_Rethrows()
	{
		// Will verify: OperationCanceledException propagates
		await Task.CompletedTask;
	}
}
