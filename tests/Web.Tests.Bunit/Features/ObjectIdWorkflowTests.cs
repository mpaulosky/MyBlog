//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     ObjectIdWorkflowTests.cs
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
using MyBlog.Web.Features.Categories.Delete;
using MyBlog.Web.Features.Categories.Edit;
using MyBlog.Web.Features.Categories.List;

using Web.Testing;

namespace Web.Features;

public class ObjectIdWorkflowTests : BunitContext
{
	private readonly TestAuthenticationStateProvider _authProvider = new();

	public ObjectIdWorkflowTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddAuthorizationCore();
		Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();
		Services.AddSingleton<AuthenticationStateProvider>(_authProvider);
		Services.AddSingleton(Substitute.For<IFileStorage>());
	}

	[Fact]
	public async Task CreatePost_Submits_SelectedCategoryString_As_ObjectId_CommandValue()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var selectedCategoryId = ObjectId.GenerateNewId();
		var categories = new[]
		{
			new CategoryDto(selectedCategoryId, "Technology", "Tech posts."),
		};

		sender.Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CategoryDto>>(categories)));
		sender.Send(Arg.Any<CreateBlogPostCommand>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(ObjectId.GenerateNewId())));

		Services.AddSingleton(sender);
		var navigation = Services.GetRequiredService<NavigationManager>();

		// Act
		var cut = RenderWithUser<Create>(CreatePrincipal("Alice", "auth0|alice", ["Author"]));
		cut.Find("input.form-input").Change("ObjectId create flow");
		await cut.InvokeAsync(() => cut.FindComponent<TextEditor>().Instance.ContentChanged.InvokeAsync("Create content"));
		cut.Find("select.form-select").Change(selectedCategoryId.ToString());
		cut.Find("form").Submit();

		// Assert
		await sender.Received(1).Send(
			Arg.Is<CreateBlogPostCommand>(command =>
				command != null &&
				command.Title == "ObjectId create flow" &&
				command.CategoryId == selectedCategoryId),
			Arg.Any<CancellationToken>());
		navigation.Uri.Should().EndWith("/blog");
	}

	[Fact]
	public async Task CreatePost_SubmittingDraftWithPlaceholderCategory_TreatsSelectionAsNoCategory()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var categories = new[]
		{
			new CategoryDto(ObjectId.GenerateNewId(), "Technology", "Tech posts."),
		};

		sender.Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CategoryDto>>(categories)));
		sender.Send(Arg.Any<CreateBlogPostCommand>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(ObjectId.GenerateNewId())));

		Services.AddSingleton(sender);
		var navigation = Services.GetRequiredService<NavigationManager>();

		// Act
		var cut = RenderWithUser<Create>(CreatePrincipal("Alice", "auth0|alice", ["Author"]));
		cut.Find("input.form-input").Change("Draft without category");
		await cut.InvokeAsync(() => cut.FindComponent<TextEditor>().Instance.ContentChanged.InvokeAsync("Draft content"));
		cut.Find("select.form-select").Change(string.Empty);
		Action act = () => cut.Find("form").Submit();

		// Assert
		act.Should().NotThrow();
		await sender.Received(1).Send(
			Arg.Is<CreateBlogPostCommand>(command =>
				command != null &&
				command.Title == "Draft without category" &&
				command.CategoryId == null &&
				!command.IsPublished),
			Arg.Any<CancellationToken>());
		cut.Markup.Should().NotContain("Please select a valid category.");
		navigation.Uri.Should().EndWith("/blog");
	}

	[Fact]
	public async Task CreatePost_PublishingWithPlaceholderCategory_ShowsValidationInsteadOfThrowing()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var categories = new[]
		{
			new CategoryDto(ObjectId.GenerateNewId(), "Technology", "Tech posts."),
		};

		sender.Send(Arg.Any<GetCategoriesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CategoryDto>>(categories)));

		Services.AddSingleton(sender);

		// Act
		var cut = RenderWithUser<Create>(CreatePrincipal("Alice", "auth0|alice", ["Author"]));
		cut.Find("input.form-input").Change("Published without category");
		await cut.InvokeAsync(() => cut.FindComponent<TextEditor>().Instance.ContentChanged.InvokeAsync("Publish me"));
		cut.Find("input[type='checkbox']").Change(true);
		cut.Find("select.form-select").Change(string.Empty);
		Action act = () => cut.Find("form").Submit();

		// Assert
		act.Should().NotThrow();
		cut.WaitForAssertion(() => cut.Markup.Should().Contain("Please select a category before publishing."));
		await sender.DidNotReceive().Send(Arg.Any<CreateBlogPostCommand>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public void EditPost_Submits_SelectedCategoryString_As_ObjectId_CommandValue()
	{
		// Arrange
		var sender = new TestSender();
		var postId = ObjectId.GenerateNewId();
		var initialCategoryId = ObjectId.GenerateNewId();
		var selectedCategoryId = ObjectId.GenerateNewId();
		const string OwnerSub = "auth0|owner-user";

		var categories = new[]
		{
			new CategoryDto(initialCategoryId, "Technology", "Tech posts."),
			new CategoryDto(selectedCategoryId, "Design", "Design posts."),
		};
		var post = new BlogPostDto(
			postId,
			"Existing title",
			"Existing content",
			OwnerSub,
			"Alice",
			string.Empty,
			[],
			DateTime.UtcNow,
			null,
			false,
			initialCategoryId);

		sender.Register<GetCategoriesQuery, Result<IReadOnlyList<CategoryDto>>>(
			Result.Ok<IReadOnlyList<CategoryDto>>(categories));
		sender.Register<GetBlogPostByIdQuery, Result<BlogPostDto?>>(
			Result.Ok<BlogPostDto?>(post));
		sender.Register<EditBlogPostCommand, Result>(Result.Ok());

		Services.AddSingleton<ISender>(sender);
		var navigation = Services.GetRequiredService<NavigationManager>();

		// Act
		var cut = RenderWithUser<Edit>(
			CreatePrincipal("Alice", OwnerSub, ["Author"]),
			parameters => parameters.Add(p => p.Id, postId.ToString()));
		cut.Find("select.form-select").Change(selectedCategoryId.ToString());
		cut.Find("form").Submit();

		// Assert
		sender.ReceivedCount<EditBlogPostCommand>(command =>
			command.Id == postId &&
			command.CategoryId == selectedCategoryId).Should().Be(1);
		navigation.Uri.Should().EndWith("/blog");
	}

	[Fact]
	public void EditPost_ClearingCategory_Submits_CommandThatRemovesCategoryAssociation()
	{
		// Arrange
		var sender = new TestSender();
		var postId = ObjectId.GenerateNewId();
		var initialCategoryId = ObjectId.GenerateNewId();
		const string OwnerSub = "auth0|owner-user";

		var categories = new[]
		{
			new CategoryDto(initialCategoryId, "Technology", "Tech posts."),
		};
		var post = new BlogPostDto(
			postId,
			"Existing title",
			"Existing content",
			OwnerSub,
			"Alice",
			string.Empty,
			[],
			DateTime.UtcNow,
			null,
			false,
			initialCategoryId);

		sender.Register<GetCategoriesQuery, Result<IReadOnlyList<CategoryDto>>>(
			Result.Ok<IReadOnlyList<CategoryDto>>(categories));
		sender.Register<GetBlogPostByIdQuery, Result<BlogPostDto?>>(
			Result.Ok<BlogPostDto?>(post));
		sender.Register<EditBlogPostCommand, Result>(Result.Ok());

		Services.AddSingleton<ISender>(sender);
		var navigation = Services.GetRequiredService<NavigationManager>();

		// Act
		var cut = RenderWithUser<Edit>(
			CreatePrincipal("Alice", OwnerSub, ["Author"]),
			parameters => parameters.Add(p => p.Id, postId.ToString()));
		cut.Find("select.form-select").Change(string.Empty);
		cut.Find("form").Submit();

		// Assert
		sender.ReceivedCount<EditBlogPostCommand>(command =>
			command.Id == postId &&
			command.CategoryId == null &&
			command.ClearCategory).Should().Be(1);
		navigation.Uri.Should().EndWith("/blog");
	}

	[Fact]
	public void CategoryIndex_Submits_EditCategoryCommand_With_Category_ObjectId()
	{
		// Arrange
		var sender = new TestSender();
		var category = new CategoryDto(ObjectId.GenerateNewId(), "Technology", "Tech posts.");

		sender.Register<GetCategoriesQuery, Result<IReadOnlyList<CategoryDto>>>(
			Result.Ok<IReadOnlyList<CategoryDto>>([category]));
		sender.Register<EditCategoryCommand, Result>(Result.Ok());

		Services.AddSingleton<ISender>(sender);

		// Act
		var cut = RenderWithUser<MyBlog.Web.Features.Categories.List.Index>(
			CreatePrincipal("Admin User", "auth0|admin-user", ["Admin"]));
		cut.FindAll("button")
			.Single(button => string.Equals(button.TextContent.Trim(), "Edit", StringComparison.Ordinal))
			.Click();
		cut.Find("tbody form").Submit();

		// Assert
		sender.ReceivedCount<EditCategoryCommand>(command => command.Id == category.Id).Should().Be(1);
	}

	[Fact]
	public void CategoryIndex_Submits_DeleteCategoryCommand_With_Category_ObjectId()
	{
		// Arrange
		var sender = new TestSender();
		var category = new CategoryDto(ObjectId.GenerateNewId(), "Technology", "Tech posts.");

		sender.RegisterSequence<GetCategoriesQuery, Result<IReadOnlyList<CategoryDto>>>(
			Result.Ok<IReadOnlyList<CategoryDto>>([category]),
			Result.Ok<IReadOnlyList<CategoryDto>>(Array.Empty<CategoryDto>()));
		sender.Register<DeleteCategoryCommand, Result>(Result.Ok());

		Services.AddSingleton<ISender>(sender);

		// Act
		var cut = RenderWithUser<MyBlog.Web.Features.Categories.List.Index>(
			CreatePrincipal("Admin User", "auth0|admin-user", ["Admin"]));
		cut.FindAll("button")
			.Single(button => string.Equals(button.TextContent.Trim(), "Delete", StringComparison.Ordinal))
			.Click();
		cut.FindAll("button")
			.Last(button => string.Equals(button.TextContent.Trim(), "Delete", StringComparison.Ordinal))
			.Click();

		// Assert
		sender.ReceivedCount<DeleteCategoryCommand>(command => command.Id == category.Id).Should().Be(1);
		cut.WaitForAssertion(() => cut.Markup.Should().Contain("No categories yet."));
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

	private static ClaimsPrincipal CreatePrincipal(string name, string sub, string[] roles)
	{
		var claims = new List<Claim>
		{
			new(ClaimTypes.Name, name),
			new("sub", sub),
		};
		claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
		return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, ClaimTypes.Role));
	}
}
