using FluentAssertions;
using MyBlog.Domain.Entities;
using MyBlog.Integration.Tests.Infrastructure;
using MyBlog.Web.Data;

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
}
