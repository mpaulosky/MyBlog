//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     EditCategoryHandler.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using MyBlog.Domain.Abstractions;

namespace MyBlog.Web.Features.Categories.Edit;

internal sealed class EditCategoryHandler(ICategoryRepository repo)
	: IRequestHandler<EditCategoryCommand, Result>
{
	public async Task<Result> Handle(
		EditCategoryCommand request,
		CancellationToken cancellationToken)
	{
		try
		{
			var category = await repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
			if (category is null)
			{
				return Result.Fail($"Category {request.Id} not found.", ResultErrorCode.NotFound);
			}

			var nameExists = await repo.ExistsByNameExcludingAsync(
				request.Name, request.Id, cancellationToken).ConfigureAwait(false);
			if (nameExists)
			{
				return Result.Fail(
					$"A category named '{request.Name.Trim()}' already exists.",
					ResultErrorCode.Conflict);
			}

			category.Update(request.Name, request.Description);
			await repo.UpdateAsync(category, cancellationToken).ConfigureAwait(false);
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
