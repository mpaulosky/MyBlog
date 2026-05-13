//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     MarkdownEditorLifecycleTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests.Bunit
//=======================================================

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using MyBlog.Domain.Abstractions;
using MyBlog.Web.Components.Shared;
using MyBlog.Web.Features.BlogPosts.Create;
using MyBlog.Web.Features.BlogPosts.Edit;

using Web.Testing;

namespace Web.Features;

/// <summary>
/// Sprint 19 interop/lifecycle validation — #327.
/// Covers the four gaps not addressed by existing smoke and ACL suites:
///   1. Create page TextEditor initialises with empty content.
///   2. Edit page TextEditor initialises with the post's existing content (explicit lifecycle assertion).
///   3. Create page disposes cleanly when navigating away mid-edit.
///   4. Edit page disposes cleanly when navigating away mid-edit.
///   5. TextEditor renders standalone with JSInterop Loose mode without propagating a JS exception.
/// All tests use JSRuntimeMode.Loose so that MarkdownEditor's EasyMDE initialisation and
/// disposal JS calls are satisfied without a real browser.
/// </summary>
public class MarkdownEditorLifecycleTests : BunitContext
{
	private readonly TestAuthenticationStateProvider _authProvider = new();

	public MarkdownEditorLifecycleTests()
	{
		// Loose mode: all unknown JS invocations (EasyMDE init, getSelectedText, etc.) return
		// default values instead of throwing — this is the correct posture for TextEditor tests.
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddAuthorizationCore();
		Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();
		Services.AddSingleton<AuthenticationStateProvider>(_authProvider);
		Services.AddSingleton(Substitute.For<IFileStorage>());
	}

	// ── Focus area 2: Lifecycle — Create initialises with empty content ─────────

	[Fact]
	public void CreateEditorInitializesWithEmptyContent()
	{
		// Arrange
		Services.AddSingleton(Substitute.For<ISender>());

		// Act
		var cut = RenderWithUser<Create>(CreateAuthorPrincipal("Alice", "auth0|alice"));

		// Assert — TextEditor.Content must be the empty string, not null or stale data
		cut.FindComponent<TextEditor>().Instance.Content.Should().BeEmpty();
	}

	// ── Focus area 2: Lifecycle — Edit initialises editor WITH existing content ─

	[Fact]
	public void EditEditorInitializesWithExistingPostContent()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var postId = Guid.NewGuid();
		const string OwnerSub = "auth0|alice";
		const string ExpectedContent = "# Sprint 19\n\nMarkdown content here.";

		var post = new BlogPostDto(postId, "Sprint Post", ExpectedContent, OwnerSub, "Alice", string.Empty, [], DateTime.UtcNow, null, false);

		sender.Send(Arg.Any<GetBlogPostByIdQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<BlogPostDto?>(post)));

		Services.AddSingleton(sender);

		// Act
		var cut = RenderWithUser<Edit>(
				CreateAuthorPrincipal("Alice", OwnerSub),
				parameters => parameters.Add(p => p.Id, postId));

		// Assert — editor must carry the post's existing content, NOT be blank
		cut.FindComponent<TextEditor>().Instance.Content.Should().Be(ExpectedContent);
	}

	// ── Focus area 3: Navigation safety — Create disposes without exception ─────

	[Fact]
	public async Task CreatePageDisposesWithoutException()
	{
		// Arrange
		Services.AddSingleton(Substitute.For<ISender>());
		RenderWithUser<Create>(CreateAuthorPrincipal("Alice", "auth0|alice"));

		// Act — simulate navigating away by disposing all rendered components
		Func<Task> act = DisposeComponentsAsync;

		// Assert — no ObjectDisposedException or JS interop exception from EasyMDE teardown
		await act.Should().NotThrowAsync();
	}

	// ── Focus area 3: Navigation safety — Edit disposes without exception ───────

	[Fact]
	public async Task EditPageDisposesWithoutException()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var postId = Guid.NewGuid();
		const string OwnerSub = "auth0|alice";

		var post = new BlogPostDto(postId, "Some Post", "Some content", OwnerSub, "Alice", string.Empty, [], DateTime.UtcNow, null, false);

		sender.Send(Arg.Any<GetBlogPostByIdQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<BlogPostDto?>(post)));

		Services.AddSingleton(sender);

		RenderWithUser<Edit>(
				CreateAuthorPrincipal("Alice", OwnerSub),
				parameters => parameters.Add(p => p.Id, postId));

		// Act — simulate navigating away mid-edit
		Func<Task> act = DisposeComponentsAsync;

		// Assert — EasyMDE JS disposal must not surface as a test exception
		await act.Should().NotThrowAsync();
	}

	// ── Focus area 1: JS interop — standalone TextEditor with Loose mode ────────

	[Fact]
	public void TextEditorRendersStandaloneWithLooseJsInteropWithoutThrow()
	{
		// Arrange — JSInterop.Mode = JSRuntimeMode.Loose already set in constructor.
		// This explicitly documents that the MarkdownEditor (EasyMDE) initialisation JS
		// call is covered by Loose mode and must not propagate as a test failure.

		// Act
		Action act = () => Render<TextEditor>(parameters => parameters
				.Add(p => p.Content, string.Empty)
				.Add(p => p.ContentChanged, EventCallback.Factory.Create<string>(this, _ => { }))
				.Add(p => p.AlignmentOptionsEnabled, false));

		// Assert
		act.Should().NotThrow();
	}

	// ── Helpers ──────────────────────────────────────────────────────────────────

	private IRenderedComponent<TComponent> RenderWithUser<TComponent>(
			ClaimsPrincipal principal,
			Action<ComponentParameterCollectionBuilder<TComponent>>? configure = null)
			where TComponent : IComponent
	{
		_authProvider.SetUser(principal);
		return Render<TComponent>(parameters =>
		{
			parameters.AddCascadingValue(Task.FromResult(new AuthenticationState(principal)));
			configure?.Invoke(parameters);
		});
	}

	/// <summary>Creates a principal with both <c>sub</c> and <c>name</c> claims so that
	/// Create.razor (reads <c>name</c>) and Edit.razor (reads <c>sub</c>) both resolve
	/// the user correctly in tests.</summary>
	private static ClaimsPrincipal CreateAuthorPrincipal(string name, string sub)
	{
		var claims = new List<Claim>
		{
			new(ClaimTypes.Name, name),
			new("sub", sub),
			new(ClaimTypes.Role, "Author"),
		};
		return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, ClaimTypes.Role));
	}
}
