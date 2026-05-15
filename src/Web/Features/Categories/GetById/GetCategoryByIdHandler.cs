//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     GetCategoryByIdHandler.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using MyBlog.Domain.Abstractions;

namespace MyBlog.Web.Features.Categories.GetById;

internal sealed class GetCategoryByIdHandler(ICategoryRepository repo)
	: IRequestHandler<GetCategoryByIdQuery, Result<CategoryDto?>>
{
	public async Task<Result<CategoryDto?>> Handle(
		GetCategoryByIdQuery request,
		CancellationToken cancellationToken)
	{
		try
		{
			var category = await repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
			return Result.Ok<CategoryDto?>(category?.ToDto());
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (InvalidOperationException ex)
		{
			return Result.Fail<CategoryDto?>(ex.Message);
		}
#pragma warning disable CA1031 // Intentional: top-level handler converts unexpected failures to Result to keep UI stable
		catch (Exception)
		{
			return Result.Fail<CategoryDto?>("An unexpected error occurred.");
		}
#pragma warning restore CA1031
	}
}
