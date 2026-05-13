//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     FileStorageModels.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

namespace MyBlog.Web.Infrastructure.FileStorage;

/// <summary>
/// Carries a file stream and its associated metadata for storage operations.
/// </summary>
internal sealed record FileData(Stream Content, FileMetaData Metadata);

/// <summary>
/// Descriptive metadata attached to a stored file.
/// </summary>
internal sealed record FileMetaData(string FileName, string ContentType, DateTime LastModified);
