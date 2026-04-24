//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     IBlogPostRepository.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain
//=======================================================

using MyBlog.Domain.Entities;

namespace MyBlog.Domain.Interfaces;

public interface IBlogPostRepository
{
	Task<BlogPost?> GetByIdAsync(Guid id, CancellationToken ct = default);
	Task<IReadOnlyList<BlogPost>> GetAllAsync(CancellationToken ct = default);
	Task AddAsync(BlogPost post, CancellationToken ct = default);
	Task UpdateAsync(BlogPost post, CancellationToken ct = default);
	Task DeleteAsync(Guid id, CancellationToken ct = default);
}
