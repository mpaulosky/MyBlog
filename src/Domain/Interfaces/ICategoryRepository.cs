//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     ICategoryRepository.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain
//=======================================================

using MyBlog.Domain.Entities;

namespace MyBlog.Domain.Interfaces;

public interface ICategoryRepository
{
	Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default);
	Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);
	Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
	Task<bool> ExistsByNameExcludingAsync(string name, Guid excludedId, CancellationToken ct = default);
	Task AddAsync(Category category, CancellationToken ct = default);
	Task UpdateAsync(Category category, CancellationToken ct = default);
	Task DeleteAsync(Guid id, CancellationToken ct = default);
}
