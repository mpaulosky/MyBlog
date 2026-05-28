//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     MongoDbBlogPostRepository.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

namespace MyBlog.Web.Data;

internal sealed class MongoDbBlogPostRepository(IDbContextFactory<BlogDbContext> contextFactory)
		: IBlogPostRepository
{
	public async Task<BlogPost?> GetByIdAsync(ObjectId id, CancellationToken ct = default)
	{
		var ctx = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
		await using (ctx.ConfigureAwait(false))
		{
			return await ctx.BlogPosts.AsNoTracking()
				.FirstOrDefaultAsync(p => p.Id == id, ct).ConfigureAwait(false);
		}
	}

	public async Task<IReadOnlyList<BlogPost>> GetAllAsync(CancellationToken ct = default)
	{
		var ctx = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
		await using (ctx.ConfigureAwait(false))
		{
			return await ctx.BlogPosts.AsNoTracking()
				.OrderByDescending(p => p.CreatedAt)
				.ToListAsync(ct).ConfigureAwait(false);
		}
	}

	public async Task<bool> ExistsByCategoryAsync(ObjectId categoryId, CancellationToken ct = default)
	{
		var ctx = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
		await using (ctx.ConfigureAwait(false))
		{
			return await ctx.BlogPosts.AsNoTracking()
				.AnyAsync(p => p.CategoryId == categoryId, ct).ConfigureAwait(false);
		}
	}

	public async Task AddAsync(BlogPost post, CancellationToken ct = default)
	{
		var ctx = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
		await using (ctx.ConfigureAwait(false))
		{
			await ctx.BlogPosts.AddAsync(post, ct).ConfigureAwait(false);
			await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
		}
	}

	public async Task UpdateAsync(BlogPost post, CancellationToken ct = default)
	{
		var ctx = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
		await using (ctx.ConfigureAwait(false))
		{
			var entry = ctx.Attach(post);
			// Version was incremented by post.Update(); the original value in the DB is Version - 1.
			// EF Core uses OriginalValue in the WHERE filter to detect concurrent modifications.
			entry.Property(p => p.Version).OriginalValue = post.Version - 1;
			entry.State = EntityState.Modified;
			await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
		}
	}

	public async Task DeleteAsync(ObjectId id, CancellationToken ct = default)
	{
		var ctx = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
		await using (ctx.ConfigureAwait(false))
		{
			var post = await ctx.BlogPosts.FindAsync([id], ct).ConfigureAwait(false);
			if (post is not null)
			{
				ctx.BlogPosts.Remove(post);
				await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
			}
		}
	}
}
