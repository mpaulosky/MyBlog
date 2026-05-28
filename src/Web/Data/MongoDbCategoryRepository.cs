//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     MongoDbCategoryRepository.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

namespace MyBlog.Web.Data;

internal sealed class MongoDbCategoryRepository(IDbContextFactory<BlogDbContext> contextFactory)
	: ICategoryRepository
{
	public async Task<Category?> GetByIdAsync(ObjectId id, CancellationToken ct = default)
	{
		var ctx = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
		await using (ctx.ConfigureAwait(false))
		{
			return await ctx.Categories.AsNoTracking()
				.FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);
		}
	}

	public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
	{
		var ctx = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
		await using (ctx.ConfigureAwait(false))
		{
			return await ctx.Categories.AsNoTracking()
				.OrderBy(c => c.Name)
				.ToListAsync(ct).ConfigureAwait(false);
		}
	}

	public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
	{
		var ctx = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
		await using (ctx.ConfigureAwait(false))
		{
			var normalizedName = name.Trim();
			return await ctx.Categories.AsNoTracking()
				.AnyAsync(c => c.Name == normalizedName, ct).ConfigureAwait(false);
		}
	}

	public async Task<bool> ExistsByNameExcludingAsync(string name, ObjectId excludedId, CancellationToken ct = default)
	{
		var ctx = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
		await using (ctx.ConfigureAwait(false))
		{
			var normalizedName = name.Trim();
			return await ctx.Categories.AsNoTracking()
				.AnyAsync(c => c.Name == normalizedName && c.Id != excludedId, ct).ConfigureAwait(false);
		}
	}

	public async Task AddAsync(Category category, CancellationToken ct = default)
	{
		var ctx = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
		await using (ctx.ConfigureAwait(false))
		{
			await ctx.Categories.AddAsync(category, ct).ConfigureAwait(false);
			await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
		}
	}

	public async Task UpdateAsync(Category category, CancellationToken ct = default)
	{
		var ctx = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
		await using (ctx.ConfigureAwait(false))
		{
			ctx.Categories.Update(category);
			await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
		}
	}

	public async Task DeleteAsync(ObjectId id, CancellationToken ct = default)
	{
		var ctx = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
		await using (ctx.ConfigureAwait(false))
		{
			var category = await ctx.Categories.FindAsync([id], ct).ConfigureAwait(false);
			if (category is not null)
			{
				ctx.Categories.Remove(category);
				await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
			}
		}
	}
}
