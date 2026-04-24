//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     ValidationBehavior.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain
//=======================================================

using FluentValidation;

using MediatR;

using MyBlog.Domain.Abstractions;

namespace MyBlog.Domain.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
	: IPipelineBehavior<TRequest, TResponse>
	where TRequest : IRequest<TResponse>
	where TResponse : Result
{
	public async Task<TResponse> Handle(
		TRequest request,
		RequestHandlerDelegate<TResponse> next,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(next);

		if (!validators.Any())
			return await next(cancellationToken).ConfigureAwait(false);

		var context = new ValidationContext<TRequest>(request);
		var failures = validators
			.Select(v => v.Validate(context))
			.SelectMany(r => r.Errors)
			.Where(f => f is not null)
			.ToList();

		if (failures.Count > 0)
		{
			var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));
			return (TResponse)CreateFailResult(typeof(TResponse), errorMessage);
		}

		return await next(cancellationToken).ConfigureAwait(false);
	}

	private static object CreateFailResult(Type resultType, string errorMessage)
	{
		if (resultType == typeof(Result))
			return Result.Fail(errorMessage, ResultErrorCode.Validation);

		// Result<T> — get generic arg and call Result.Fail<T>(...)
		var valueType = resultType.GetGenericArguments()[0];
		var method = typeof(Result)
			.GetMethods()
			.First(m => m is { Name: "Fail", IsGenericMethodDefinition: true } && m.GetParameters().Length == 2);
		return method.MakeGenericMethod(valueType).Invoke(null, [errorMessage, ResultErrorCode.Validation])!;
	}
}
