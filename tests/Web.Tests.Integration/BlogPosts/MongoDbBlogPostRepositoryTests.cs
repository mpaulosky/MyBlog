//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     MongoDbBlogPostRepositoryTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests.Integration
//=======================================================

using Web.Infrastructure;

namespace Web.BlogPosts;

[Collection("BlogPostIntegration")]
public sealed class MongoDbBlogPostRepositoryTests(MongoDbFixture fixture)
{
	private MongoDbBlogPostRepository CreateRepo(string? dbName = null) =>
		new(fixture.CreateFactory(dbName ?? $"T{Guid.NewGuid():N}"));

	[Fact]
	public async Task AddAsync_persists_post_to_MongoDB()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		var repo = CreateRepo();
		var post = BlogPost.Create("Hello World", "Some content", "Author A");

		// Act
		await repo.AddAsync(post, ct);

		// Assert
		var all = await repo.GetAllAsync(ct);
		all.Should().HaveCount(1);
		all[0].Id.Should().Be(post.Id);
		all[0].Title.Should().Be("Hello World");
	}

	[Fact]
	public async Task GetByIdAsync_returns_null_when_not_found()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		var repo = CreateRepo();

		// Act
		var result = await repo.GetByIdAsync(Guid.NewGuid(), ct);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetByIdAsync_returns_post_when_found()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		var repo = CreateRepo();
		var post = BlogPost.Create("My Title", "My Content", "My Author");
		await repo.AddAsync(post, ct);

		// Act
		var result = await repo.GetByIdAsync(post.Id, ct);

		// Assert
		result.Should().NotBeNull();
		result!.Title.Should().Be("My Title");
		result.Author.Should().Be("My Author");
		result.Content.Should().Be("My Content");
	}

	[Fact]
	public async Task GetAllAsync_returns_posts_ordered_by_newest_first()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		var repo = CreateRepo();
		var older = BlogPost.Create("Older Post", "Content", "Author");
		await repo.AddAsync(older, ct);

		await Task.Delay(20, ct);

		var newer = BlogPost.Create("Newer Post", "Content", "Author");
		await repo.AddAsync(newer, ct);

		// Act
		var all = await repo.GetAllAsync(ct);

		// Assert
		all.Should().HaveCount(2);
		all[0].Title.Should().Be("Newer Post");
		all[1].Title.Should().Be("Older Post");
	}

	[Fact]
	public async Task UpdateAsync_modifies_post_in_MongoDB()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		var repo = CreateRepo();
		var post = BlogPost.Create("Original Title", "Original Content", "Author");
		await repo.AddAsync(post, ct);

		post.Update("Updated Title", "Updated Content");

		// Act
		await repo.UpdateAsync(post, ct);

		// Assert
		var result = await repo.GetByIdAsync(post.Id, ct);
		result!.Title.Should().Be("Updated Title");
		result.Content.Should().Be("Updated Content");
	}

	[Fact]
	public async Task DeleteAsync_removes_post_from_MongoDB()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		var repo = CreateRepo();
		var post = BlogPost.Create("To Delete", "Content", "Author");
		await repo.AddAsync(post, ct);

		// Act
		await repo.DeleteAsync(post.Id, ct);

		// Assert
		var all = await repo.GetAllAsync(ct);
		all.Should().BeEmpty();
	}

	[Fact]
	public async Task DeleteAsync_does_nothing_when_post_not_found()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		var repo = CreateRepo();

		// Act
		var act = async () => await repo.DeleteAsync(Guid.NewGuid(), ct);

		// Assert
		await act.Should().NotThrowAsync();
	}

	[Fact]
	public async Task GetAllAsync_returns_empty_when_no_posts_exist()
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
	public async Task UpdateAsync_throws_when_version_conflicts_with_concurrent_update()
	{
		// Arrange
		var ct = TestContext.Current.CancellationToken;
		var dbName = $"T{Guid.NewGuid():N}";
		var repo1 = CreateRepo(dbName);
		var repo2 = CreateRepo(dbName);
		var post = BlogPost.Create("Original", "Content", "Author");
		await repo1.AddAsync(post, ct);

		var winner = await repo2.GetByIdAsync(post.Id, ct) ?? throw new InvalidOperationException("post not found");
		winner.Update("Winner Title", "Winner Content");
		await repo2.UpdateAsync(winner, ct);

		post.Update("Late Title", "Late Content");

		// Act
		var act = async () => await repo1.UpdateAsync(post, ct);

		// Assert
		await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
	}
}
