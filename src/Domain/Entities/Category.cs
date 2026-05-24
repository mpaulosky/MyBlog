//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     Category.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain
//=======================================================

using MongoDB.Bson;

namespace MyBlog.Domain.Entities;

public sealed class Category
{
	public ObjectId Id { get; private set; }
	public string Name { get; private set; } = string.Empty;
	public string Description { get; private set; } = string.Empty;

	private Category() { }

	public static Category Create(string name, string description)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		ArgumentException.ThrowIfNullOrWhiteSpace(description);

		return new Category
		{
			Id = ObjectId.GenerateNewId(),
			Name = name.Trim(),
			Description = description.Trim(),
		};
	}

	public void Update(string name, string description)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		ArgumentException.ThrowIfNullOrWhiteSpace(description);
		Name = name.Trim();
		Description = description.Trim();
	}
}
