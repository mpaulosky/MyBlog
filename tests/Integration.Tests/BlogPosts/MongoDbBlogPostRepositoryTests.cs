//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     MongoDbBlogPostRepositoryTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Integration.Tests
//=======================================================

using MyBlog.Domain.Entities;
using MyBlog.Integration.Tests.Infrastructure;

namespace MyBlog.Integration.Tests.BlogPosts;

[Collection("MongoDb")]
public sealed class MongoDbBlogPostRepositoryTests(MongoDbFixture fixture)
		: IClassFixture<MongoDbFixture>
{
	private MongoDbBlogPostRepository CreateRepo() =>
			new(fixture.CreateFactory($"blog_{Guid.NewGuid():N}"));

	[Fact]
	public async Task AddAsync_persists_post_to_MongoDB()
	{
		var repo = CreateRepo();
		var post = BlogPost.Create("Hello World", "Some content", "Author A");

		await repo.AddAsync(post);

		var all = await repo.GetAllAsync();
		all.Should().HaveCount(1);
		all[0].Id.Should().Be(post.Id);
		all[0].Title.Should().Be("Hello World");
	}

	[Fact]
	public async Task GetByIdAsync_returns_null_when_not_found()
	{
		var repo = CreateRepo();

		var result = await repo.GetByIdAsync(Guid.NewGuid());

		result.Should().BeNull();
	}

	[Fact]
	public async Task GetByIdAsync_returns_post_when_found()
	{
		var repo = CreateRepo();
		var post = BlogPost.Create("My Title", "My Content", "My Author");
		await repo.AddAsync(post);

		var result = await repo.GetByIdAsync(post.Id);

		result.Should().NotBeNull();
		result!.Title.Should().Be("My Title");
		result.Author.Should().Be("My Author");
		result.Content.Should().Be("My Content");
	}

	[Fact]
	public async Task GetAllAsync_returns_posts_ordered_by_newest_first()
	{
		var repo = CreateRepo();
		var older = BlogPost.Create("Older Post", "Content", "Author");
		await repo.AddAsync(older);

		await Task.Delay(20);

		var newer = BlogPost.Create("Newer Post", "Content", "Author");
		await repo.AddAsync(newer);

		var all = await repo.GetAllAsync();

		all.Should().HaveCount(2);
		all[0].Title.Should().Be("Newer Post");
		all[1].Title.Should().Be("Older Post");
	}

	[Fact]
	public async Task UpdateAsync_modifies_post_in_MongoDB()
	{
		var repo = CreateRepo();
		var post = BlogPost.Create("Original Title", "Original Content", "Author");
		await repo.AddAsync(post);

		post.Update("Updated Title", "Updated Content");
		await repo.UpdateAsync(post);

		var result = await repo.GetByIdAsync(post.Id);
		result!.Title.Should().Be("Updated Title");
		result.Content.Should().Be("Updated Content");
	}

	[Fact]
	public async Task DeleteAsync_removes_post_from_MongoDB()
	{
		var repo = CreateRepo();
		var post = BlogPost.Create("To Delete", "Content", "Author");
		await repo.AddAsync(post);

		await repo.DeleteAsync(post.Id);

		var all = await repo.GetAllAsync();
		all.Should().BeEmpty();
	}

	[Fact]
	public async Task DeleteAsync_does_nothing_when_post_not_found()
	{
		var repo = CreateRepo();

		var act = async () => await repo.DeleteAsync(Guid.NewGuid());

		await act.Should().NotThrowAsync();
	}

	[Fact]
	public async Task GetAllAsync_returns_empty_when_no_posts_exist()
	{
		var repo = CreateRepo();

		var all = await repo.GetAllAsync();

		all.Should().BeEmpty();
	}

	[Fact]
	public async Task UpdateAsync_throws_when_version_conflicts_with_concurrent_update()
	{
		// Arrange – two repos targeting the same database
		var dbName = $"blog_{Guid.NewGuid():N}";
		var repo1 = new MongoDbBlogPostRepository(fixture.CreateFactory(dbName));
		var repo2 = new MongoDbBlogPostRepository(fixture.CreateFactory(dbName));

		// Insert via repo1 — Version == 0 in DB and in the entity
		var post = BlogPost.Create("Original", "Content", "Author");
		await repo1.AddAsync(post);

		// repo2 wins the race: reads, updates (Version → 1 in entity), saves — DB Version becomes 1
		var winner = await repo2.GetByIdAsync(post.Id) ?? throw new InvalidOperationException("post not found");
		winner.Update("Winner Title", "Winner Content");
		await repo2.UpdateAsync(winner);

		// Simulate user from repo1 applying their own edit (Version → 1 in entity, but DB is already 1)
		post.Update("Late Title", "Late Content");

		// Act — repo1 tries to save (OriginalValue = 0) but DB already has Version = 1
		var act = async () => await repo1.UpdateAsync(post);

		// Assert — EF Core detects the Version mismatch and throws
		await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
	}
}
