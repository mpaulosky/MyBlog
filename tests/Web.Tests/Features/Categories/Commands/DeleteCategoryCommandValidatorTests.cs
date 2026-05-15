//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     DeleteCategoryCommandValidatorTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

// Staged #339 — awaiting DeleteCategoryCommand + DeleteCategoryCommandValidator from Sam.
// Remove [Skip] attributes and reference types once the Delete slice lands.

namespace Web.Features.Categories.Commands;

public class DeleteCategoryCommandValidatorTests
{
	// ── Staged: validator for Delete command ─────────────────────────────
	// Unblock once MyBlog.Web.Features.Categories.Delete types are committed.

	[Fact(Skip = "Staged #339: awaiting DeleteCategoryCommand + DeleteCategoryCommandValidator")]
	public void Validate_ValidId_ReturnsNoErrors()
	{
		// Will verify: DeleteCategoryCommand(Guid.NewGuid()) → IsValid == true
	}

	[Fact(Skip = "Staged #339: awaiting DeleteCategoryCommand + DeleteCategoryCommandValidator")]
	public void Validate_EmptyGuid_ReturnsIdError()
	{
		// Will verify: DeleteCategoryCommand(Guid.Empty) → IsValid == false, error on "Id"
	}

	[Fact(Skip = "Staged #339: awaiting DeleteCategoryCommand + DeleteCategoryCommandValidator")]
	public void Validate_EmptyGuid_ReturnsRequiredMessage()
	{
		// Will verify: error message is "Id is required."
	}
}
