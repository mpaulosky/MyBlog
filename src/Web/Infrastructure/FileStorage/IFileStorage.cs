//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     IFileStorage.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

namespace MyBlog.Web.Infrastructure.FileStorage;

/// <summary>
/// Abstracts file persistence so components remain decoupled from the underlying store.
/// </summary>
internal interface IFileStorage
{
	/// <summary>
	/// Persists <paramref name="file"/> and returns the stored filename that callers
	/// can use to build a retrieval URL (e.g. <c>/api/files/{filename}</c>).
	/// </summary>
	Task<string> AddFileAsync(FileData file);
}
