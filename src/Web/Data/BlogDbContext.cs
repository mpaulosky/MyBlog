//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     BlogDbContext.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using System.Diagnostics.CodeAnalysis;

using MongoDB.EntityFrameworkCore.Extensions;

namespace MyBlog.Web.Data;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "The DbContext type is part of the web composition root and shared test infrastructure.")]
public sealed class BlogDbContext(DbContextOptions<BlogDbContext> options) : DbContext(options)
{
	public DbSet<BlogPost> BlogPosts => Set<BlogPost>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		ArgumentNullException.ThrowIfNull(modelBuilder);

		var entity = modelBuilder.Entity<BlogPost>();
		entity.ToCollection("blogposts");
		entity.HasKey(p => p.Id);
		entity.Property(p => p.Version).IsConcurrencyToken();
	}
}
