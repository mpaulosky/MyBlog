//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     BlogPost.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain
//=======================================================

using MongoDB.Bson;

using MyBlog.Domain.ValueObjects;

namespace MyBlog.Domain.Entities;

public sealed class BlogPost
{
	public ObjectId Id { get; private set; }
	public string Title { get; private set; } = string.Empty;
	public string Content { get; private set; } = string.Empty;
	public PostAuthor Author { get; private set; } = PostAuthor.Empty;
	public DateTime CreatedAt { get; private set; }
	public DateTime? UpdatedAt { get; private set; }
	public bool IsPublished { get; private set; }
	public int Version { get; private set; }
	public ObjectId? CategoryId { get; private set; }

	private BlogPost() { }

	public static BlogPost Create(string title, string content, PostAuthor author)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(title);
		ArgumentException.ThrowIfNullOrWhiteSpace(content);
		ArgumentNullException.ThrowIfNull(author);
		ArgumentException.ThrowIfNullOrWhiteSpace(author.Name);

		return new BlogPost
		{
			Id = ObjectId.GenerateNewId(),
			Title = title,
			Content = content,
			Author = author,
			CreatedAt = DateTime.UtcNow,
		};
	}

	public void Update(string title, string content, ObjectId? categoryId = null, bool clearCategory = false)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(title);
		ArgumentException.ThrowIfNullOrWhiteSpace(content);
		Title = title;
		Content = content;
		UpdatedAt = DateTime.UtcNow;
		if (categoryId.HasValue)
		{
			CategoryId = categoryId.Value;
		}
		else if (clearCategory)
		{
			CategoryId = null;
		}
		Version++;
	}

	public void Publish() => IsPublished = true;
	public void Unpublish() => IsPublished = false;

	public void AssignCategory(ObjectId categoryId)
	{
		CategoryId = categoryId;
		Version++;
	}

	public void RemoveCategory()
	{
		CategoryId = null;
		Version++;
	}
}
