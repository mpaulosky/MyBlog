//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     Result.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Domain
//=======================================================

// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     Result.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : MyBlog
// Project Name :  Domain
// =======================================================

namespace MyBlog.Domain.Abstractions;

public enum ResultErrorCode
{
	None = 0,
	Concurrency = 1,
	NotFound = 2,
	Validation = 3,
	Conflict = 4
}

public class Result
{
	protected Result(bool success, string? errorMessage = null, ResultErrorCode errorCode = ResultErrorCode.None, object? details = null)
	{
		Success = success;
		Error = errorMessage;
		ErrorCode = errorCode;
		Details = details;
	}

	public bool Success { get; }

	public bool Failure => !Success;

	public string? Error { get; }

	public ResultErrorCode ErrorCode { get; }

	/// <summary>
	/// Optional structured error details (e.g., server version on concurrency conflict).
	/// </summary>
	public object? Details { get; }

	public static Result Ok()
	{
		return new Result(true);
	}

	public static Result Fail(string errorMessage)
	{
		return new Result(false, errorMessage);
	}

	public static Result Fail(string errorMessage, ResultErrorCode code)
	{
		return new Result(false, errorMessage, code);
	}

	public static Result Fail(string errorMessage, ResultErrorCode code, object? details)
	{
		return new Result(false, errorMessage, code, details);
	}

	public static Result<T> Ok<T>(T value)
	{
		return new Result<T>(value, true);
	}

	public static Result<T> Fail<T>(string errorMessage)
	{
		return new Result<T>(default, false, errorMessage);
	}

	public static Result<T> Fail<T>(string errorMessage, ResultErrorCode code)
	{
		return new Result<T>(default, false, errorMessage, code);
	}

	public static Result<T> Fail<T>(string errorMessage, ResultErrorCode code, object? details)
	{
		return new Result<T>(default, false, errorMessage, code, details);
	}

	public static Result<T> FromValue<T>(T? value)
	{
		return value is not null ? Ok(value) : Result<T>.Fail("Provided value is null.");
	}

}

public sealed class Result<T> : Result
{
	internal Result(T? value, bool success, string? errorMessage = null, ResultErrorCode errorCode = ResultErrorCode.None, object? details = null)
		: base(success, errorMessage, errorCode, details)
	{
		Value = value;
	}

	public T? Value { get; }

	public T? ToValue() => Value;

	public static Result<T> FromValue(T? value) => Ok(value);

	private static Result<T> Ok(T? value)
	{
		return new Result<T>(value, true);
	}

#pragma warning disable CA1000
	public static new Result<T> Fail(string errorMessage)
	{
		return new Result<T>(default, false, errorMessage);
	}

	public static new Result<T> Fail(string errorMessage, ResultErrorCode code)
	{
		return new Result<T>(default, false, errorMessage, code);
	}

	public static new Result<T> Fail(string errorMessage, ResultErrorCode code, object? details)
	{
		return new Result<T>(default, false, errorMessage, code, details);
	}
#pragma warning restore CA1000

	public static implicit operator T?(Result<T>? result)
	{
		if (result is null)
		{
			// Return the language default for T? when the Result is null. For value types this will
			// be the underlying default (e.g., 0 for int) which matches existing behavior.
			return default;
		}

		return result.Value;
	}

	public static implicit operator Result<T>(T? value)
	{
		return Ok(value);
	}
}
