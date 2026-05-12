//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     HtmlSanitizerBehaviorTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests
//=======================================================

using Ganss.Xss;

using Microsoft.Extensions.Logging.Abstractions;

using MyBlog.Web.Features.BlogPosts.Create;
using MyBlog.Web.Features.BlogPosts.Edit;

namespace Web.Handlers;

/// <summary>
/// Verifies handler behavior when the sanitizer strips or empties content,
/// and validates that the real HtmlSanitizer removes dangerous markup.
/// </summary>
public class HtmlSanitizerBehaviorTests
{
	private readonly IBlogPostRepository _repo = Substitute.For<IBlogPostRepository>();
	private readonly IBlogPostCacheService _cache = Substitute.For<IBlogPostCacheService>();
	private readonly IHtmlSanitizer _sanitizer = Substitute.For<IHtmlSanitizer>();

	// ── Create handler – sanitizer path ──────────────────────────────────────

	[Fact]
	public async Task CreateHandle_SanitizationChangesContent_StoresCleanContent()
	{
		// Arrange
		const string DirtyHtml = "<p>Hello</p><script>evil()</script>";
		const string CleanHtml = "<p>Hello</p>";
		_sanitizer.Sanitize(DirtyHtml).Returns(CleanHtml);

		BlogPost? captured = null;
		_repo.AddAsync(Arg.Do<BlogPost>(p => captured = p), Arg.Any<CancellationToken>())
			.Returns(Task.CompletedTask);

		var handler = new CreateBlogPostHandler(_repo, _cache, _sanitizer, NullLogger<CreateBlogPostHandler>.Instance);
		var command = new CreateBlogPostCommand("Title", DirtyHtml, new PostAuthor("", "Author", "", []));

		// Act
		var result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		captured.Should().NotBeNull();
		captured!.Content.Should().Be(CleanHtml);
	}

	[Fact]
	public async Task CreateHandle_ContentEmptyAfterSanitization_ReturnsFailWithMessage()
	{
		// Arrange
		const string MaliciousOnly = "<script>evil()</script>";
		_sanitizer.Sanitize(MaliciousOnly).Returns(string.Empty);

		var handler = new CreateBlogPostHandler(_repo, _cache, _sanitizer, NullLogger<CreateBlogPostHandler>.Instance);
		var command = new CreateBlogPostCommand("Title", MaliciousOnly, new PostAuthor("", "Author", "", []));

		// Act
		var result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("empty after sanitization");
		await _repo.DidNotReceive().AddAsync(Arg.Any<BlogPost>(), Arg.Any<CancellationToken>());
	}

	// ── Edit handler – sanitizer path ────────────────────────────────────────

	[Fact]
	public async Task EditHandle_SanitizationChangesContent_StoresCleanContent()
	{
		// Arrange
		const string DirtyHtml = "<p>Updated</p><iframe src='evil.com'></iframe>";
		const string CleanHtml = "<p>Updated</p>";
		_sanitizer.Sanitize(DirtyHtml).Returns(CleanHtml);

		var authorId = "auth0|author1";
		var post = BlogPost.Create("Original", "Content", new PostAuthor(authorId, "Author 1", "", []));
		_repo.GetByIdAsync(post.Id, Arg.Any<CancellationToken>()).Returns(post);

		var handler = new EditBlogPostHandler(_repo, _cache, _sanitizer, NullLogger<EditBlogPostHandler>.Instance);
		var command = new EditBlogPostCommand(post.Id, "Updated Title", DirtyHtml, authorId, false);

		// Act
		var result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		post.Content.Should().Be(CleanHtml);
	}

	[Fact]
	public async Task EditHandle_ContentEmptyAfterSanitization_ReturnsFailWithMessage()
	{
		// Arrange
		const string MaliciousOnly = "<script>xss()</script>";
		_sanitizer.Sanitize(MaliciousOnly).Returns(string.Empty);

		var handler = new EditBlogPostHandler(_repo, _cache, _sanitizer, NullLogger<EditBlogPostHandler>.Instance);
		var command = new EditBlogPostCommand(Guid.NewGuid(), "Title", MaliciousOnly, "auth0|user", false);

		// Act
		var result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("empty after sanitization");
		await _repo.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
	}

	// ── Real HtmlSanitizer integration ───────────────────────────────────────

	[Fact]
	public void RealSanitizer_ScriptTagsRemoved_ReturnsCleanContent()
	{
		// Arrange
		var sanitizer = new HtmlSanitizer();
		const string Input = "<p>Hello World</p><script>alert('xss')</script>";

		// Act
		var result = sanitizer.Sanitize(Input);

		// Assert
		result.Should().NotContain("<script>");
		result.Should().Contain("Hello World");
	}

	[Fact]
	public void RealSanitizer_JavaScriptEventHandlerAttributes_AreStripped()
	{
		// Arrange
		var sanitizer = new HtmlSanitizer();
		const string Input = "<p onclick=\"evil()\">Click me</p>";

		// Act
		var result = sanitizer.Sanitize(Input);

		// Assert
		result.Should().NotContain("onclick");
		result.Should().Contain("Click me");
	}

	[Fact]
	public void RealSanitizer_SafeHtmlTags_ArePreserved()
	{
		// Arrange
		var sanitizer = new HtmlSanitizer();
		const string Input = "<p><strong>Bold</strong> and <em>italic</em></p>";

		// Act
		var result = sanitizer.Sanitize(Input);

		// Assert
		result.Should().Contain("<strong>");
		result.Should().Contain("<em>");
		result.Should().Contain("Bold");
		result.Should().Contain("italic");
	}
}
