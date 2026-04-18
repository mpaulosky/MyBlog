namespace MyBlog.Web.Data;

public sealed class MongoDbBlogPostRepository(IDbContextFactory<BlogDbContext> contextFactory)
		: IBlogPostRepository
{
	public async Task<BlogPost?> GetByIdAsync(Guid id, CancellationToken ct = default)
	{
		await using var ctx = await contextFactory.CreateDbContextAsync(ct);
		return await ctx.BlogPosts.AsNoTracking()
				.FirstOrDefaultAsync(p => p.Id == id, ct);
	}

	public async Task<IReadOnlyList<BlogPost>> GetAllAsync(CancellationToken ct = default)
	{
		await using var ctx = await contextFactory.CreateDbContextAsync(ct);
		return await ctx.BlogPosts.AsNoTracking()
				.OrderByDescending(p => p.CreatedAt)
				.ToListAsync(ct);
	}

	public async Task AddAsync(BlogPost post, CancellationToken ct = default)
	{
		await using var ctx = await contextFactory.CreateDbContextAsync(ct);
		await ctx.BlogPosts.AddAsync(post, ct);
		await ctx.SaveChangesAsync(ct);
	}

	public async Task UpdateAsync(BlogPost post, CancellationToken ct = default)
	{
		await using var ctx = await contextFactory.CreateDbContextAsync(ct);
		var entry = ctx.Attach(post);
		// Version was incremented by post.Update(); the original value in the DB is Version - 1.
		// EF Core uses OriginalValue in the WHERE filter to detect concurrent modifications.
		entry.Property(p => p.Version).OriginalValue = post.Version - 1;
		entry.State = EntityState.Modified;
		await ctx.SaveChangesAsync(ct);
	}

	public async Task DeleteAsync(Guid id, CancellationToken ct = default)
	{
		await using var ctx = await contextFactory.CreateDbContextAsync(ct);
		var post = await ctx.BlogPosts.FindAsync([id], ct);
		if (post is not null)
		{
			ctx.BlogPosts.Remove(post);
			await ctx.SaveChangesAsync(ct);
		}
	}
}
