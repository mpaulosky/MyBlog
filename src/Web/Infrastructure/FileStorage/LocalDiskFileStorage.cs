//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     LocalDiskFileStorage.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

namespace MyBlog.Web.Infrastructure.FileStorage;

/// <summary>
/// Saves uploaded files to <c>wwwroot/uploads</c> on the local disk.
/// Intended as a baseline implementation; swap for a cloud-backed store in production.
/// </summary>
internal sealed partial class LocalDiskFileStorage : IFileStorage
{
	private static readonly string[] AllowedExtensions =
		[".JPG", ".JPEG", ".PNG", ".GIF", ".WEBP"];

	private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

	private readonly IWebHostEnvironment _env;
	private readonly ILogger<LocalDiskFileStorage> _logger;

	[LoggerMessage(Level = LogLevel.Information, Message = "File saved: {FileName}")]
	private static partial void LogFileSaved(ILogger logger, string fileName);

	public LocalDiskFileStorage(IWebHostEnvironment env, ILogger<LocalDiskFileStorage> logger)
	{
		_env = env;
		_logger = logger;
	}

	/// <inheritdoc/>
	public async Task<string> AddFileAsync(FileData file)
	{
		if (string.IsNullOrEmpty(_env.WebRootPath))
		{
			throw new InvalidOperationException("WebRootPath is not configured.");
		}

		if (file.Content.Length > MaxFileSizeBytes)
		{
			throw new InvalidOperationException("File exceeds the maximum allowed size of 10 MB.");
		}

		var extension = Path.GetExtension(file.Metadata.FileName).ToUpperInvariant();
		if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
		{
			throw new InvalidOperationException(
				$"File type '{extension}' is not allowed. Only images are permitted.");
		}

		var uploadsPath = Path.Combine(_env.WebRootPath, "uploads");
		Directory.CreateDirectory(uploadsPath);

		var uniqueName = $"{Guid.NewGuid()}{extension}";
		var filePath = Path.Combine(uploadsPath, uniqueName);

		var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
		try
		{
			await file.Content.CopyToAsync(fs).ConfigureAwait(false);

			LogFileSaved(_logger, uniqueName);
			return uniqueName;
		}
		finally
		{
			await fs.DisposeAsync().ConfigureAwait(false);
		}
	}
}
