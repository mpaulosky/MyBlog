//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     MongoDbCategoryRepositoryTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests.Integration
//=======================================================

using Web.Infrastructure;

namespace Web.Categories;

[Collection("CategoryIntegration")]
public sealed class MongoDbCategoryRepositoryTests(MongoDbFixture fixture)
{
	private MongoDbCategoryRepository CreateRepo(string? dbName = null) =>
		new(fixture.CreateFactory(dbName ?? $"T{Guid.NewGuid():N}"));

	// ── AddAsync / GetAllAsync ────────────────────────────────────────────

	[Fact]
	public async Task AddAsync_PersistsCategoryToMongoDB()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		var repo = CreateRepo();
		var category = Category.Create("Technology", "Posts about technology.");

		// Act
		await repo.AddAsync(category, ct);

		// Assert
		var all = await repo.GetAllAsync(ct);
		all.Should().HaveCount(1);
		all[0].Id.Should().Be(category.Id);
		all[0].Name.Should().Be("Technology");
		all[0].Description.Should().Be("Posts about technology.");
	}

	[Fact]
	public async Task GetAllAsync_ReturnsEmptyWhenNoCategories()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		var repo = CreateRepo();

		// Act
		var all = await repo.GetAllAsync(ct);

		// Assert
		all.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAllAsync_ReturnsOrderedAlphabeticallyByName()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		var repo = CreateRepo();
		await repo.AddAsync(Category.Create("Zebra", "Z description."), ct);
		await repo.AddAsync(Category.Create("Alpha", "A description."), ct);
		await repo.AddAsync(Category.Create("Middle", "M description."), ct);

		// Act
		var all = await repo.GetAllAsync(ct);

		// Assert — categories returned alphabetically ascending
		all.Should().HaveCount(3);
		all[0].Name.Should().Be("Alpha");
		all[1].Name.Should().Be("Middle");
		all[2].Name.Should().Be("Zebra");
	}

	// ── GetByIdAsync ──────────────────────────────────────────────────────

	[Fact]
	public async Task GetByIdAsync_ReturnsNullWhenNotFound()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		var repo = CreateRepo();

		// Act
		var result = await repo.GetByIdAsync(ObjectId.GenerateNewId(), ct);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetByIdAsync_ReturnsCategoryWhenFound()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		var repo = CreateRepo();
		var category = Category.Create("Design", "Design and UX posts.");
		await repo.AddAsync(category, ct);

		// Act
		var result = await repo.GetByIdAsync(category.Id, ct);

		// Assert
		result.Should().NotBeNull();
		result!.Name.Should().Be("Design");
		result.Description.Should().Be("Design and UX posts.");
	}

	[Fact]
	public async Task GetByIdAsync_ReturnsNullForDifferentObjectIdWhenRepositoryContainsCategories()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		var repo = CreateRepo();
		var category = Category.Create("Design", "Design and UX posts.");
		await repo.AddAsync(category, ct);

		// Act
		var result = await repo.GetByIdAsync(ObjectId.GenerateNewId(), ct);

		// Assert
		result.Should().BeNull();
	}

	// ── UpdateAsync ───────────────────────────────────────────────────────

	[Fact]
	public async Task UpdateAsync_ModifiesCategoryInMongoDB()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		var repo = CreateRepo();
		var category = Category.Create("Old Name", "Old description.");
		await repo.AddAsync(category, ct);
		category.Update("New Name", "New description.");

		// Act
		await repo.UpdateAsync(category, ct);

		// Assert
		var result = await repo.GetByIdAsync(category.Id, ct);
		result!.Name.Should().Be("New Name");
		result.Description.Should().Be("New description.");
	}

	[Fact]
	public async Task UpdateAsync_OnlyChangesCategoryMatchingObjectId()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		var repo = CreateRepo();
		var updatedCategory = Category.Create("Technology", "Tech posts.");
		var untouchedCategory = Category.Create("Design", "Design posts.");
		await repo.AddAsync(updatedCategory, ct);
		await repo.AddAsync(untouchedCategory, ct);
		updatedCategory.Update("Technology Updated", "Updated tech posts.");

		// Act
		await repo.UpdateAsync(updatedCategory, ct);

		// Assert
		var reloadedUpdatedCategory = await repo.GetByIdAsync(updatedCategory.Id, ct);
		var reloadedUntouchedCategory = await repo.GetByIdAsync(untouchedCategory.Id, ct);
		reloadedUpdatedCategory.Should().NotBeNull();
		reloadedUpdatedCategory!.Name.Should().Be("Technology Updated");
		reloadedUpdatedCategory.Description.Should().Be("Updated tech posts.");
		reloadedUntouchedCategory.Should().NotBeNull();
		reloadedUntouchedCategory!.Name.Should().Be("Design");
		reloadedUntouchedCategory.Description.Should().Be("Design posts.");
	}

	// ── DeleteAsync ───────────────────────────────────────────────────────

	[Fact]
	public async Task DeleteAsync_RemovesCategoryFromMongoDB()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		var repo = CreateRepo();
		var category = Category.Create("ToDelete", "Will be removed.");
		await repo.AddAsync(category, ct);

		// Act
		await repo.DeleteAsync(category.Id, ct);

		// Assert
		var all = await repo.GetAllAsync(ct);
		all.Should().BeEmpty();
	}

	[Fact]
	public async Task DeleteAsync_RemovesOnlyCategoryMatchingObjectId()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		var repo = CreateRepo();
		var removedCategory = Category.Create("ToDelete", "Will be removed.");
		var keptCategory = Category.Create("ToKeep", "Will stay.");
		await repo.AddAsync(removedCategory, ct);
		await repo.AddAsync(keptCategory, ct);

		// Act
		await repo.DeleteAsync(removedCategory.Id, ct);

		// Assert
		var removedResult = await repo.GetByIdAsync(removedCategory.Id, ct);
		var keptResult = await repo.GetByIdAsync(keptCategory.Id, ct);
		removedResult.Should().BeNull();
		keptResult.Should().NotBeNull();
		keptResult!.Id.Should().Be(keptCategory.Id);
		keptResult.Name.Should().Be("ToKeep");
	}

	[Fact]
	public async Task DeleteAsync_DoesNothingWhenCategoryNotFound()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		var repo = CreateRepo();

		// Act
		var act = async () => await repo.DeleteAsync(ObjectId.GenerateNewId(), ct);

		// Assert
		await act.Should().NotThrowAsync();
	}

	// ── ExistsByNameAsync (unique name constraint) ────────────────────────

	[Fact]
	public async Task ExistsByNameAsync_ReturnsTrueWhenNameExists()
	{
		// Arrange — AC: unique category name
		var ct = TestContext.Current.CancellationToken;
		var repo = CreateRepo();
		await repo.AddAsync(Category.Create("Technology", "Tech."), ct);

		// Act
		var exists = await repo.ExistsByNameAsync("Technology", ct);

		// Assert
		exists.Should().BeTrue();
	}

	[Fact]
	public async Task ExistsByNameAsync_ReturnsFalseWhenNameNotFound()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		var repo = CreateRepo();

		// Act
		var exists = await repo.ExistsByNameAsync("NonExistent", ct);

		// Assert
		exists.Should().BeFalse();
	}

	[Fact]
	public async Task ExistsByNameAsync_IsCaseInsensitiveToTrimming()
	{
		// Arrange — repository normalizes with Trim()
		var ct = TestContext.Current.CancellationToken;
		var repo = CreateRepo();
		await repo.AddAsync(Category.Create("Technology", "Tech."), ct);

		// Act — name with padding should still match after trim
		var exists = await repo.ExistsByNameAsync("  Technology  ", ct);

		// Assert
		exists.Should().BeTrue();
	}

	// ── ExistsByNameExcludingAsync (update uniqueness check) ─────────────

	[Fact]
	public async Task ExistsByNameExcludingAsync_ReturnsTrueWhenAnotherCategoryHasSameName()
	{
		// Arrange — AC: unique category name on update
		var ct = TestContext.Current.CancellationToken;
		var repo = CreateRepo();
		var existing = Category.Create("Technology", "Tech.");
		var updating = Category.Create("Design", "Design.");
		await repo.AddAsync(existing, ct);
		await repo.AddAsync(updating, ct);

		// Act — check if "Technology" exists excluding the "updating" category
		var exists = await repo.ExistsByNameExcludingAsync("Technology", updating.Id, ct);

		// Assert
		exists.Should().BeTrue();
	}

	[Fact]
	public async Task ExistsByNameExcludingAsync_ReturnsFalseWhenOnlyMatchIsExcludedCategory()
	{
		// Arrange — same category is editing its own name (should be allowed)
		var ct = TestContext.Current.CancellationToken;
		var repo = CreateRepo();
		var category = Category.Create("Technology", "Tech.");
		await repo.AddAsync(category, ct);

		// Act — exclude the same category; "Technology" only belongs to it
		var exists = await repo.ExistsByNameExcludingAsync("Technology", category.Id, ct);

		// Assert
		exists.Should().BeFalse();
	}
}
