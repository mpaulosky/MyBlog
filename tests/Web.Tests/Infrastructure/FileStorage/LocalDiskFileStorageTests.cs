//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     LocalDiskFileStorageTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using System.Text;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

using MyBlog.Web.Infrastructure.FileStorage;

namespace Web.Infrastructure.FileStorage;

public sealed class LocalDiskFileStorageTests : IDisposable
{
	private readonly string _webRootPath = Path.Combine(Path.GetTempPath(), $"myblog-localdiskfilestorage-{Guid.NewGuid():N}");
	private readonly IWebHostEnvironment _webHostEnvironment = Substitute.For<IWebHostEnvironment>();
	private readonly ILogger<LocalDiskFileStorage> _logger = Substitute.For<ILogger<LocalDiskFileStorage>>();

	public LocalDiskFileStorageTests()
	{
		_webHostEnvironment.WebRootPath.Returns(_webRootPath);
	}

	public void Dispose()
	{
		if (Directory.Exists(_webRootPath))
		{
			Directory.Delete(_webRootPath, recursive: true);
		}
	}

	[Fact]
	public async Task AddFileAsync_WithAllowedImage_WritesFileUnderUploadsAndReturnsStoredName()
	{
		// Arrange
		var sut = new LocalDiskFileStorage(_webHostEnvironment, _logger);
		await using var content = new MemoryStream(Encoding.UTF8.GetBytes("image payload"));
		var file = new FileData(
				content,
				new FileMetaData("cover.png", "image/png", DateTime.UtcNow));

		// Act
		var storedName = await sut.AddFileAsync(file);

		// Assert
		storedName.Should().EndWith(".PNG");
		var savedPath = Path.Combine(_webRootPath, "uploads", storedName);
		File.Exists(savedPath).Should().BeTrue();
		var savedContents = await File.ReadAllTextAsync(savedPath, TestContext.Current.CancellationToken);
		savedContents.Should().Be("image payload");
	}
}
