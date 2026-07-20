//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CategoryTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain.Tests
//=======================================================

namespace Domain.Entities;

public class CategoryTests
{
	// ── Create ───────────────────────────────────────────────────────────────

	[Fact]
	public void Create_ValidNameAndDescription_ReturnsEntityWithCorrectFields()
	{
		// Arrange / Act
		var category = Category.Create("Technology", "Posts about tech topics.");

		// Assert
		category.Name.Should().Be("Technology");
		category.Description.Should().Be("Posts about tech topics.");
	}

	[Fact]
	public void Create_ValidArguments_IdIsNonEmptyObjectId()
	{
		// Arrange / Act
		var category = Category.Create("Tech", "Description.");

		// Assert
		category.Id.Should().NotBe(ObjectId.Empty);
	}

	[Fact]
	public void Create_ValidArguments_IdCanBeRoundTrippedFromString()
	{
		// Arrange / Act
		var category = Category.Create("Tech", "Description.");

		// Assert
		ObjectId.TryParse(category.Id.ToString(), out var parsedId).Should().BeTrue();
		parsedId.Should().Be(category.Id);
	}

	[Fact]
	public void Create_TrimsWhitespacePadding_FromNameAndDescription()
	{
		// Arrange / Act
		var category = Category.Create("  Tech  ", "  Some description.  ");

		// Assert
		category.Name.Should().Be("Tech");
		category.Description.Should().Be("Some description.");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Create_NullOrWhiteSpaceName_ThrowsArgumentException(string? name)
	{
		// Arrange / Act
		var act = () => Category.Create(name!, "Valid description.");

		// Assert
		act.Should().Throw<ArgumentException>();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Create_NullOrWhiteSpaceDescription_ThrowsArgumentException(string? description)
	{
		// Arrange / Act
		var act = () => Category.Create("Tech", description!);

		// Assert
		act.Should().Throw<ArgumentException>();
	}

	// ── Update ───────────────────────────────────────────────────────────────

	[Fact]
	public void Update_ValidArguments_UpdatesNameAndDescription()
	{
		// Arrange
		var category = Category.Create("Old Name", "Old description.");

		// Act
		category.Update("New Name", "New description.");

		// Assert
		category.Name.Should().Be("New Name");
		category.Description.Should().Be("New description.");
	}

	[Fact]
	public void Update_TrimsWhitespacePadding_FromNameAndDescription()
	{
		// Arrange
		var category = Category.Create("Tech", "Initial.");

		// Act
		category.Update("  Updated Name  ", "  Updated description.  ");

		// Assert
		category.Name.Should().Be("Updated Name");
		category.Description.Should().Be("Updated description.");
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Update_NullOrWhiteSpaceName_ThrowsArgumentException(string? name)
	{
		// Arrange
		var category = Category.Create("Tech", "Description.");

		// Act
		var act = () => category.Update(name!, "Valid description.");

		// Assert
		act.Should().Throw<ArgumentException>();
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Update_NullOrWhiteSpaceDescription_ThrowsArgumentException(string? description)
	{
		// Arrange
		var category = Category.Create("Tech", "Description.");

		// Act
		var act = () => category.Update("Tech", description!);

		// Assert
		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	public void Update_DoesNotAlterId_AfterUpdate()
	{
		// Arrange
		var category = Category.Create("Tech", "Description.");
		var originalId = category.Id;

		// Act
		category.Update("New Name", "New description.");

		// Assert
		category.Id.Should().Be(originalId);
	}
}
