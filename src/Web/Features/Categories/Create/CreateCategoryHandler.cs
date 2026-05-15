//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CreateCategoryHandler.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using MyBlog.Domain.Abstractions;

namespace MyBlog.Web.Features.Categories.Create;

internal sealed class CreateCategoryHandler(ICategoryRepository repo)
	: IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
	public async Task<Result<Guid>> Handle(
		CreateCategoryCommand request,
		CancellationToken cancellationToken)
	{
		try
		{
			var nameExists = await repo.ExistsByNameAsync(request.Name, cancellationToken).ConfigureAwait(false);
			if (nameExists)
			{
				return Result.Fail<Guid>(
					$"A category named '{request.Name.Trim()}' already exists.",
					ResultErrorCode.Conflict);
			}

			var category = Category.Create(request.Name, request.Description);
			await repo.AddAsync(category, cancellationToken).ConfigureAwait(false);
			return Result.Ok<Guid>(category.Id);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (InvalidOperationException ex)
		{
			return Result.Fail<Guid>(ex.Message);
		}
#pragma warning disable CA1031 // Intentional: top-level handler converts unexpected failures to Result to keep UI stable
		catch (Exception)
		{
			return Result.Fail<Guid>("An unexpected error occurred.");
		}
#pragma warning restore CA1031
	}
}
