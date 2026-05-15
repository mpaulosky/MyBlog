//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     GetCategoriesHandler.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using MyBlog.Domain.Abstractions;

namespace MyBlog.Web.Features.Categories.List;

internal sealed class GetCategoriesHandler(ICategoryRepository repo)
	: IRequestHandler<GetCategoriesQuery, Result<IReadOnlyList<CategoryDto>>>
{
	public async Task<Result<IReadOnlyList<CategoryDto>>> Handle(
		GetCategoriesQuery request,
		CancellationToken cancellationToken)
	{
		try
		{
			var categories = await repo.GetAllAsync(cancellationToken).ConfigureAwait(false);
			var dtos = (IReadOnlyList<CategoryDto>)categories.Select(c => c.ToDto()).ToList();
			return Result.Ok<IReadOnlyList<CategoryDto>>(dtos);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (InvalidOperationException ex)
		{
			return Result.Fail<IReadOnlyList<CategoryDto>>(ex.Message);
		}
#pragma warning disable CA1031 // Intentional: top-level handler converts unexpected failures to Result to keep UI stable
		catch (Exception)
		{
			return Result.Fail<IReadOnlyList<CategoryDto>>("An unexpected error occurred.");
		}
#pragma warning restore CA1031
	}
}
