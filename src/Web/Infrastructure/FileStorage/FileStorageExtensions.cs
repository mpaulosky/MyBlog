//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     FileStorageExtensions.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

namespace MyBlog.Web.Infrastructure.FileStorage;

internal static class FileStorageExtensions
{
	/// <summary>
	/// Registers <see cref="IFileStorage"/> with its <see cref="LocalDiskFileStorage"/>
	/// implementation.
	/// </summary>
	public static IServiceCollection AddFileStorage(this IServiceCollection services)
	{
		services.AddScoped<IFileStorage, LocalDiskFileStorage>();
		return services;
	}
}
