using FluentAssertions;
using MyBlog.Domain.Entities;

namespace MyBlog.Unit.Tests;

public class BlogPostTests
{
    [Fact]
    public void Create_WithValidArgs_ReturnsBlogPost()
    {
        var post = BlogPost.Create("Test Title", "Test Content", "Test Author");

        post.Id.Should().NotBeEmpty();
        post.Title.Should().Be("Test Title");
        post.Content.Should().Be("Test Content");
        post.Author.Should().Be("Test Author");
        post.IsPublished.Should().BeFalse();
        post.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("", "content", "author")]
    [InlineData("title", "", "author")]
    [InlineData("title", "content", "")]
    public void Create_WithBlankArgs_ThrowsArgumentException(string title, string content, string author)
    {
        var act = () => BlogPost.Create(title, content, author);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_ChangesTitle_AndContent()
    {
        var post = BlogPost.Create("Old Title", "Old Content", "Author");
        post.Update("New Title", "New Content");

        post.Title.Should().Be("New Title");
        post.Content.Should().Be("New Content");
        post.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Publish_SetsIsPublished_True()
    {
        var post = BlogPost.Create("T", "C", "A");
        post.Publish();
        post.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void Unpublish_SetsIsPublished_False()
    {
        var post = BlogPost.Create("T", "C", "A");
        post.Publish();
        post.Unpublish();
        post.IsPublished.Should().BeFalse();
    }
}
