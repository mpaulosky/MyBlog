//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     EditCategoryRegressionTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests.Bunit
//=======================================================

// Regression tests for PR #342 / issue #341 blockers:
//   1. Re-navigation must not leave stale _categoriesLoadFailed state on Edit page.
//   2. Publish guard must allow saving an already-categorized published post when the
//      category list fails to load; it must only block when CategoryId is null.

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using MyBlog.Domain.Abstractions;
using MyBlog.Web.Features.BlogPosts.Edit;
using MyBlog.Web.Features.Categories.List;

using Web.Testing;

namespace Web.Features;

public class EditCategoryRegressionTests : BunitContext
{
	private readonly TestAuthenticationStateProvider _authProvider = new();

	public EditCategoryRegressionTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddAuthorizationCore();
		Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();
		Services.AddSingleton<AuthenticationStateProvider>(_authProvider);
		Services.AddSingleton(Substitute.For<IFileStorage>());
	}

	// Regression: _categoriesLoadFailed was never reset in OnParametersSetAsync, so
	// a failed first load would permanently show the failure banner even after a later
	// navigation where categories load successfully.
	[Fact]
	public void EditClearsCategoryLoadFailureAfterRenavigationToPostWhoseCategoriesLoad()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var firstPostId = ObjectId.GenerateNewId();
		var secondPostId = ObjectId.GenerateNewId();
		const string OwnerSub = "auth0|owner-user";

		var firstPost = new BlogPostDto(firstPostId, "First Post", "Content", OwnerSub, "Owner",
			string.Empty, [], DateTime.UtcNow, null, false, null);
		var secondPost = new BlogPostDto(secondPostId, "Second Post", "Content", OwnerSub, "Owner",
			string.Empty, [], DateTime.UtcNow, null, false, null);
		var category = new CategoryDto(ObjectId.GenerateNewId(), "Tech", "Tech category");

		// First navigation: categories fail, post loads fine.
		sender.Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
			.Returns(
				Task.FromResult(Result.Fail<IReadOnlyList<CategoryDto>>("Category service unavailable.")),
				Task.FromResult(Result.Ok<IReadOnlyList<CategoryDto>>(new[] { category })));

		sender.Send(Arg.Is<GetBlogPostByIdQuery>(q => q.Id == firstPostId), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<BlogPostDto?>(firstPost)));
		sender.Send(Arg.Is<GetBlogPostByIdQuery>(q => q.Id == secondPostId), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<BlogPostDto?>(secondPost)));

		Services.AddSingleton(sender);

		// Act — first render: categories fail → failure banner expected.
		var cut = RenderWithUser<Edit>(
			CreatePrincipalWithSub(OwnerSub, ["Author"]),
			parameters => parameters.Add(p => p.Id, firstPostId.ToString()));

		cut.Markup.Should().Contain("Categories could not be loaded",
			because: "first load failure must surface the failure banner");

		// Act — re-navigate: categories now succeed.
		cut.Render(parameters => parameters.Add(p => p.Id, secondPostId.ToString()));

		// Assert — stale failure banner must be gone after successful reload.
		cut.Markup.Should().NotContain("Categories could not be loaded",
			because: "_categoriesLoadFailed must be reset on re-navigation; a prior failure must not persist");
		cut.Markup.Should().Contain("Second Post",
			because: "the new post content must render correctly");
	}

	// Regression: publish guard fired on _categoriesLoadFailed alone, blocking saves for
	// already-categorized published posts when the category list happened to be unavailable.
	// Correct behaviour: only block publishing when CategoryId is null.
	// The checkbox is explicitly changed to ensure IsPublished=true is bound before submit,
	// making this a true red-green test for the guard condition.
	[Fact]
	public void EditAllowsSaveOfPublishedPostThatAlreadyHasCategoryEvenWhenCategoryListFails()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var postId = ObjectId.GenerateNewId();
		var categoryId = ObjectId.GenerateNewId();
		const string OwnerSub = "auth0|owner-user";

		// Post is already published and already has a CategoryId — a real-world published post.
		var post = new BlogPostDto(postId, "Published Post", "Content", OwnerSub, "Owner",
			string.Empty, [], DateTime.UtcNow, null, IsPublished: true, CategoryId: categoryId);

		sender.Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Fail<IReadOnlyList<CategoryDto>>("Category service unavailable.")));

		sender.Send(Arg.Any<GetBlogPostByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<BlogPostDto?>(post)));

		sender.Send(Arg.Any<EditBlogPostCommand>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok()));

		Services.AddSingleton(sender);
		var navigation = Services.GetRequiredService<NavigationManager>();

		var cut = RenderWithUser<Edit>(
			CreatePrincipalWithSub(OwnerSub, ["Author"]),
			parameters => parameters.Add(p => p.Id, postId.ToString()));

		// Confirm the category failure banner is visible (_categoriesLoadFailed=true).
		cut.Markup.Should().Contain("Categories could not be loaded",
			because: "category list failure must be visible before submit");

		// Explicitly bind IsPublished=true through the checkbox change event so the guard
		// evaluates with the correct IsPublished value during HandleSubmit.
		cut.Find("input[type='checkbox']").Change(true);

		// Act — submit the edit form.
		cut.Find("button[type='submit']").Click();

		// Assert — the guard must NOT fire for an already-categorized post.
		// With the bug: guard evaluates _model.IsPublished && (_categoriesLoadFailed || ...) = true,
		// which would set _error and block navigation. Without the bug: only _model.CategoryId is
		// checked, so the command is sent and navigation happens.
		cut.WaitForAssertion(() =>
			cut.Markup.Should().NotContain("Cannot publish until categories are available",
				because: "guard must not block an already-categorized published post"));

		sender.Received(1).Send(Arg.Any<EditBlogPostCommand>(), Arg.Any<CancellationToken>());
		navigation.Uri.Should().EndWith("/blog");
	}

	// Guard rail: publish is still blocked when CategoryId is null, regardless of whether
	// the category list failed to load (cannot select a category to fix it).
	[Fact]
	public void EditBlocksPublishWhenCategoryIdIsNullAndCategoryListFailed()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var postId = ObjectId.GenerateNewId();
		const string OwnerSub = "auth0|owner-user";

		// Post is marked published but has no category — user cannot fix this without categories loading.
		var post = new BlogPostDto(postId, "Draft Post", "Content", OwnerSub, "Owner",
			string.Empty, [], DateTime.UtcNow, null, IsPublished: true, CategoryId: null);

		sender.Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Fail<IReadOnlyList<CategoryDto>>("Category service unavailable.")));

		sender.Send(Arg.Any<GetBlogPostByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<BlogPostDto?>(post)));

		Services.AddSingleton(sender);
		var navigation = Services.GetRequiredService<NavigationManager>();

		var cut = RenderWithUser<Edit>(
			CreatePrincipalWithSub(OwnerSub, ["Author"]),
			parameters => parameters.Add(p => p.Id, postId.ToString()));

		// Explicitly bind IsPublished=true so the guard evaluates the publish condition.
		cut.Find("input[type='checkbox']").Change(true);

		// Act
		cut.Find("button[type='submit']").Click();

		// Assert — publishing must be blocked because there is no CategoryId and no way to pick one.
		// Guard error message: "Categories failed to load. Cannot publish until categories are available."
		cut.WaitForAssertion(() =>
			cut.Markup.Should().Contain("Cannot publish until categories are available",
				because: "guard must block a published post with no CategoryId when category list failed"));

		navigation.Uri.Should().NotEndWith("/blog");
		sender.DidNotReceive().Send(Arg.Any<EditBlogPostCommand>(), Arg.Any<CancellationToken>());
	}

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

	private static ClaimsPrincipal CreatePrincipalWithSub(string sub, string[] roles)
	{
		var claims = new List<Claim> { new("sub", sub) };
		claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
		return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth", null, ClaimTypes.Role));
	}
}
