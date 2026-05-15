//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     UpdateCategoryCommandValidatorTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

// Staged #339 — awaiting UpdateCategoryCommand + UpdateCategoryCommandValidator from Sam.
// Remove [Skip] attributes and reference types once the Edit slice lands.

namespace Web.Features.Categories.Commands;

public class UpdateCategoryCommandValidatorTests
{
	// ── Staged: validator for Update command ─────────────────────────────
	// Unblock once MyBlog.Web.Features.Categories.Edit types are committed.

	[Fact(Skip = "Staged #339: awaiting UpdateCategoryCommand + UpdateCategoryCommandValidator")]
	public void Validate_ValidCommand_ReturnsNoErrors()
	{
		// Will verify: UpdateCategoryCommand(Guid.NewGuid(), "Tech", "Description.") → IsValid == true
	}

	[Fact(Skip = "Staged #339: awaiting UpdateCategoryCommand + UpdateCategoryCommandValidator")]
	public void Validate_EmptyId_ReturnsIdError()
	{
		// Will verify: UpdateCategoryCommand(Guid.Empty, "Tech", "Desc.") → error on "Id"
	}

	[Fact(Skip = "Staged #339: awaiting UpdateCategoryCommand + UpdateCategoryCommandValidator")]
	public void Validate_EmptyName_ReturnsNameError()
	{
		// Will verify: UpdateCategoryCommand(id, "", "Desc.") → error on "Name"
	}

	[Fact(Skip = "Staged #339: awaiting UpdateCategoryCommand + UpdateCategoryCommandValidator")]
	public void Validate_EmptyDescription_ReturnsDescriptionError()
	{
		// Will verify: UpdateCategoryCommand(id, "Tech", "") → error on "Description"
	}

	[Fact(Skip = "Staged #339: awaiting UpdateCategoryCommand + UpdateCategoryCommandValidator")]
	public void Validate_NameExceedsMaxLength_ReturnsNameError()
	{
		// Will verify: name > 100 chars → error on "Name"
	}

	[Fact(Skip = "Staged #339: awaiting UpdateCategoryCommand + UpdateCategoryCommandValidator")]
	public void Validate_DescriptionExceedsMaxLength_ReturnsDescriptionError()
	{
		// Will verify: description > 500 chars → error on "Description"
	}
}
