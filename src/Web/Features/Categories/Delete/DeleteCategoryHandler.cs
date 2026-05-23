//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     DeleteCategoryHandler.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using MyBlog.Domain.Abstractions;

namespace MyBlog.Web.Features.Categories.Delete;

internal sealed class DeleteCategoryHandler(
	ICategoryRepository categoryRepo,
	IBlogPostRepository blogPostRepo) : IRequestHandler<DeleteCategoryCommand, Result>
{
	public async Task<Result> Handle(
		DeleteCategoryCommand request,
		CancellationToken cancellationToken)
	{
		try
		{
			var category = await categoryRepo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
			if (category is null)
			{
				return Result.Fail($"Category {request.Id} not found.", ResultErrorCode.NotFound);
			}

			var inUse = await blogPostRepo.ExistsByCategoryAsync(request.Id, cancellationToken).ConfigureAwait(false);
			if (inUse)
			{
				return Result.Fail(
					$"Category '{category.Name}' cannot be deleted because it is assigned to one or more blog posts.",
					ResultErrorCode.Conflict);
			}

			await categoryRepo.DeleteAsync(request.Id, cancellationToken).ConfigureAwait(false);
			return Result.Ok();
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (InvalidOperationException ex)
		{
			return Result.Fail(ex.Message);
		}
#pragma warning disable CA1031 // Intentional: top-level handler converts unexpected failures to Result to keep UI stable
		catch (Exception)
		{
			return Result.Fail("An unexpected error occurred.");
		}
#pragma warning restore CA1031
	}
}
