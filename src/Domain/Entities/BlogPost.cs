//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     BlogPost.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain
//=======================================================

namespace MyBlog.Domain.Entities;

public sealed class BlogPost
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public string Author { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsPublished { get; private set; }
    public int Version { get; private set; }

    private BlogPost() { }

    public static BlogPost Create(string title, string content, string author)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(author);

        return new BlogPost
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = content,
            Author = author,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void Update(string title, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        Title = title;
        Content = content;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void Publish() => IsPublished = true;
    public void Unpublish() => IsPublished = false;
}
