//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     RazorSmokeTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests.Bunit
//=======================================================

using MediatR;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using MyBlog.Domain.Abstractions;
using MyBlog.Web.Components.Layout;
using MyBlog.Web.Components.Pages;
using MyBlog.Web.Components.Shared;
using MyBlog.Web.Features.BlogPosts.Create;
using MyBlog.Web.Features.BlogPosts.Delete;
using MyBlog.Web.Features.BlogPosts.Edit;
using MyBlog.Web.Features.BlogPosts.List;
using MyBlog.Web.Features.UserManagement;

using Web.Testing;

namespace Web.Components;

public class RazorSmokeTests : BunitContext
{
	private readonly TestAuthenticationStateProvider _authProvider = new();

	public RazorSmokeTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddAuthorizationCore();
		Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();
		Services.AddSingleton<AuthenticationStateProvider>(_authProvider);
		Services.AddSingleton(Substitute.For<IFileStorage>());
	}

	[Fact]
	public void HomeRendersWelcomeMessage()
	{
		// Arrange (none)
		// Act
		var cut = Render<Home>();

		// Assert
		cut.Markup.Should().Contain("Hello, users!");
		cut.Markup.Should().Contain("Welcome to your new app.");
	}

	[Fact]
	public void ErrorUsesCascadingHttpContextTraceIdentifier()
	{
		// Arrange
		var httpContext = new DefaultHttpContext
		{
			TraceIdentifier = "trace-123"
		};

		// Act
		var cut = Render<Error>(parameters => parameters.AddCascadingValue(httpContext));

		// Assert
		cut.Markup.Should().Contain("trace-123");
	}

	[Fact]
	public void NotFoundRendersNotFoundMessage()
	{
		// Arrange (none)
		// Act
		var cut = Render<NotFound>();

		// Assert
		cut.Markup.Should().Contain("Not Found");
		cut.Markup.Should().Contain("does not exist");
	}

	[Fact]
	public void ConfirmDeleteDialogShowsDialogWhenVisible()
	{
		// Arrange (none)
		// Act
		var cut = Render<ConfirmDeleteDialog>(parameters => parameters
				.Add(p => p.IsVisible, true)
				.Add(p => p.PostTitle, "My Post"));

		// Assert
		cut.Markup.Should().Contain("Confirm Delete");
		cut.Markup.Should().Contain("My Post");
	}

	[Fact]
	public void ConfirmDeleteDialogUsesDestructiveAndSecondaryButtonVariants()
	{
		// Arrange (none)
		// Act
		var cut = Render<ConfirmDeleteDialog>(parameters => parameters
				.Add(p => p.IsVisible, true)
				.Add(p => p.PostTitle, "My Post"));

		var deleteButton = cut.FindAll("button")
				.Single(button => string.Equals(button.TextContent.Trim(), "Delete", StringComparison.Ordinal));
		var cancelButton = cut.FindAll("button")
				.Single(button => string.Equals(button.TextContent.Trim(), "Cancel", StringComparison.Ordinal));

		// Assert
		deleteButton.GetAttribute("class").Should().Contain("btn-destructive");
		cancelButton.GetAttribute("class").Should().Contain("btn-secondary");
	}

	[Fact]
	public void RedirectToLoginNavigatesToLoginWithReturnUrl()
	{
		// Arrange
		var navigation = Services.GetRequiredService<NavigationManager>();

		// Act
		Render<RedirectToLogin>();

		// Assert
		navigation.Uri.Should().Contain("/Account/Login?returnUrl=");
	}

	[Fact]
	public void MainLayoutRendersMainContentTargetAndFooter()
	{
		// Arrange (none)
		// Act
		var cut = RenderWithUser<MainLayout>(CreatePrincipal("Layout User", ["Author"]), parameters => parameters
				.Add(layout => layout.Body, (RenderFragment)(builder => builder.AddContent(0, "Body content"))));

		// Assert
		cut.Markup.Should().Contain("id=\"main-content\"");
		cut.Markup.Should().Contain("Body content");
		cut.Markup.Should().Contain("Training Project");
		cut.Find("footer").GetAttribute("role").Should().Be("contentinfo");
	}

	[Fact]
	public void BlogIndexRendersPostsForAuthorizedUserAndCanOpenDeleteDialog()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var posts = new[]
		{
						new BlogPostDto(Guid.NewGuid(), "First", "Content", string.Empty, "Alice", string.Empty, [], DateTime.UtcNow, null, false, null)
				};

		sender.Send(Arg.Any<GetBlogPostsQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<IReadOnlyList<BlogPostDto>>(posts)));

		Services.AddSingleton(sender);

		// Act
		var cut = RenderWithUser<MyBlog.Web.Features.BlogPosts.List.Index>(CreatePrincipal("Alice", ["Author"]));

		// Assert
		var heading = cut.Find("header h1");

		heading.TextContent.Trim().Should().Be("Blog Posts");
		heading.GetAttribute("class").Should().Contain("text-primary-900").And.Contain("dark:text-primary-50");
		cut.Markup.Should().Contain("First");
		cut.Markup.Should().Contain("Edit");
		cut.Find("button").Click();
		cut.Markup.Should().Contain("Confirm Delete");
	}

	[Fact]
	public void BlogIndexUsesPrimaryAndSecondaryButtonVariantsForAuthorActions()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var posts = new[]
		{
						new BlogPostDto(Guid.NewGuid(), "First", "Content", string.Empty, "Alice", string.Empty, [], DateTime.UtcNow, null, false, null)
				};

		sender.Send(Arg.Any<GetBlogPostsQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<IReadOnlyList<BlogPostDto>>(posts)));

		Services.AddSingleton(sender);

		// Act
		var cut = RenderWithUser<MyBlog.Web.Features.BlogPosts.List.Index>(CreatePrincipal("Alice", ["Author"]));
		var createLink = cut.Find("a[href='/blog/create']");
		var editLink = cut.Find($"a[href='/blog/edit/{posts[0].Id}']");

		// Assert
		createLink.GetAttribute("class").Should().Contain("btn-primary");
		editLink.GetAttribute("class").Should().Contain("btn-secondary");
	}

	[Fact]
	public void BlogIndexUsesBtnDestructiveForInlineDeleteButton()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var posts = new[]
		{
						new BlogPostDto(Guid.NewGuid(), "First", "Content", string.Empty, "Alice", string.Empty, [], DateTime.UtcNow, null, false, null)
				};

		sender.Send(Arg.Any<GetBlogPostsQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<IReadOnlyList<BlogPostDto>>(posts)));

		Services.AddSingleton(sender);

		// Act
		var cut = RenderWithUser<MyBlog.Web.Features.BlogPosts.List.Index>(CreatePrincipal("Alice", ["Author"]));

		var deleteButton = cut.FindAll("button")
				.Single(button => string.Equals(button.TextContent.Trim(), "Delete", StringComparison.Ordinal));

		// Assert — inline delete must use the shared destructive variant, not raw Tailwind classes
		deleteButton.GetAttribute("class").Should().Contain("btn-destructive");
	}

	[Fact]
	public void BlogIndexShowsEmptyStateWhenNoPostsExist()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		sender.Send(Arg.Any<GetBlogPostsQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<IReadOnlyList<BlogPostDto>>(Array.Empty<BlogPostDto>())));

		Services.AddSingleton(sender);

		// Act
		var cut = RenderWithUser<MyBlog.Web.Features.BlogPosts.List.Index>(CreatePrincipal("Alice", ["Author"]));

		// Assert
		cut.Markup.Should().Contain("No posts yet.");
	}

	[Fact]
	public void BlogIndexShowsDismissibleErrorWhenInitialLoadFails()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		sender.Send(Arg.Any<GetBlogPostsQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Fail<IReadOnlyList<BlogPostDto>>("Unable to load posts.")));

		Services.AddSingleton(sender);

		// Act
		var cut = RenderWithUser<MyBlog.Web.Features.BlogPosts.List.Index>(CreatePrincipal("Alice", ["Author"]));

		// Assert
		cut.Markup.Should().Contain("Unable to load posts.");
		cut.Find("button.alert-dismiss").Click();
		cut.Markup.Should().NotContain("Unable to load posts.");
	}

	[Fact]
	public void BlogIndexConfirmDeleteSendsDeleteCommandAndRefreshesList()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var postId = Guid.NewGuid();
		var posts = new[]
		{
						new BlogPostDto(postId, "First", "Content", string.Empty, "Alice", string.Empty, [], DateTime.UtcNow, null, false, null)
				};

		sender.Send(Arg.Any<GetBlogPostsQuery>(), Arg.Any<CancellationToken>())
				.Returns(
						Task.FromResult(Result.Ok<IReadOnlyList<BlogPostDto>>(posts)),
						Task.FromResult(Result.Ok<IReadOnlyList<BlogPostDto>>(Array.Empty<BlogPostDto>())));
		sender.Send(Arg.Any<DeleteBlogPostCommand>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok()));

		Services.AddSingleton(sender);

		// Act
		var cut = RenderWithUser<MyBlog.Web.Features.BlogPosts.List.Index>(CreatePrincipal("Alice", ["Author"]));

		cut.Find("button").Click();
		cut.FindAll("button").Last(button => button.TextContent.Contains("Delete")).Click();

		// Assert
		sender.Received(1).Send(Arg.Is<DeleteBlogPostCommand>(command => command.Id == postId), Arg.Any<CancellationToken>());
		cut.WaitForAssertion(() => cut.Markup.Should().Contain("No posts yet."));
	}

	[Fact]
	public void BlogIndexShowsConcurrencyWarningWhenDeleteFailsWithConcurrency()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var postId = Guid.NewGuid();
		var posts = new[]
		{
						new BlogPostDto(postId, "First", "Content", string.Empty, "Alice", string.Empty, [], DateTime.UtcNow, null, false, null)
				};

		sender.Send(Arg.Any<GetBlogPostsQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<IReadOnlyList<BlogPostDto>>(posts)));
		sender.Send(Arg.Any<DeleteBlogPostCommand>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Fail("Concurrency error", ResultErrorCode.Concurrency)));

		Services.AddSingleton(sender);

		// Act
		var cut = RenderWithUser<MyBlog.Web.Features.BlogPosts.List.Index>(CreatePrincipal("Alice", ["Author"]));

		cut.Find("button").Click();
		cut.FindAll("button").Last(button => button.TextContent.Contains("Delete")).Click();

		// Assert
		cut.WaitForAssertion(() =>
		{
			cut.Markup.Should().Contain("Concurrency Conflict");
			cut.Markup.Should().Contain("Concurrency error");
		});
	}

	[Fact]
	public void CreatePostRendersForm()
	{
		// Arrange
		Services.AddSingleton(Substitute.For<ISender>());

		// Act
		var cut = RenderWithUser<Create>(CreatePrincipal("Alice", ["Author"]));

		// Assert
		var heading = cut.Find("header h1");

		heading.TextContent.Trim().Should().Be("Create Post");
		heading.GetAttribute("class").Should().Contain("text-primary-900").And.Contain("dark:text-primary-50");
		cut.FindAll("input").Count.Should().BeGreaterThanOrEqualTo(1);
		cut.FindComponent<TextEditor>();
	}

	[Fact]
	public void CreatePostShowsMarkdownContentLabel()
	{
		// Arrange
		Services.AddSingleton(Substitute.For<ISender>());

		// Act
		var cut = RenderWithUser<Create>(CreatePrincipal("Alice", ["Author"]));

		// Assert — label text and Markdown hint must be present (UX parity with Edit page)
		cut.Markup.Should().Contain("Content");
		cut.Markup.Should().Contain("(Markdown)");
	}

	[Fact]
	public void CreatePostUsesPrimaryAndSecondaryButtonVariants()
	{
		// Arrange
		Services.AddSingleton(Substitute.For<ISender>());

		// Act
		var cut = RenderWithUser<Create>(CreatePrincipal("Alice", ["Author"]));
		var submitButton = cut.Find("button[type='submit']");
		var cancelLink = cut.Find("a[href='/blog']");

		// Assert
		submitButton.GetAttribute("class").Should().Contain("btn-primary");
		cancelLink.GetAttribute("class").Should().Contain("btn-secondary");
	}

	[Fact]
	public async Task CreatePostSubmitsAndNavigatesToBlogWhenCommandSucceeds()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		sender.Send(Arg.Any<CreateBlogPostCommand>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok(Guid.NewGuid())));
		Services.AddSingleton(sender);

		var navigation = Services.GetRequiredService<NavigationManager>();

		// Act
		var cut = RenderWithUser<Create>(CreatePrincipal("Alice", ["Author"]));

		cut.FindAll("input")[0].Change("My title");
		var textEditor = cut.FindComponent<TextEditor>();
		await cut.InvokeAsync(() => textEditor.Instance.ContentChanged.InvokeAsync("Hello world"));
		cut.Find("form").Submit();

		// Assert
		await sender.Received(1).Send(Arg.Is<CreateBlogPostCommand>(command =>
				command.Title == "My title" &&
				command.Author.Name == "Alice" &&
				command.Content == "Hello world"), Arg.Any<CancellationToken>());
		navigation.Uri.Should().EndWith("/blog");
	}

	[Fact]
	public async Task CreatePostShowsDismissibleErrorWhenCommandFails()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		sender.Send(Arg.Any<CreateBlogPostCommand>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Fail<Guid>("Unable to create post.")));
		Services.AddSingleton(sender);

		// Act
		var cut = RenderWithUser<Create>(CreatePrincipal("Alice", ["Author"]));

		cut.FindAll("input")[0].Change("My title");
		var textEditor = cut.FindComponent<TextEditor>();
		await cut.InvokeAsync(() => textEditor.Instance.ContentChanged.InvokeAsync("Hello world"));
		cut.Find("form").Submit();

		// Assert
		cut.Markup.Should().Contain("Unable to create post.");
		cut.Find("button.alert-dismiss").Click();
		cut.Markup.Should().NotContain("Unable to create post.");
	}

	[Fact]
	public void EditPostLoadsExistingPost()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var postId = Guid.NewGuid();
		var post = new BlogPostDto(postId, "Existing title", "Existing content", string.Empty, "Alice", string.Empty, [], DateTime.UtcNow, null, false, null);

		sender.Send(Arg.Any<GetBlogPostByIdQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<BlogPostDto?>(post)));

		Services.AddSingleton(sender);

		// Act
		var cut = RenderWithUser<Edit>(CreatePrincipal("Alice", ["Author"]), parameters => parameters.Add(p => p.Id, postId));

		// Assert
		cut.Markup.Should().Contain("Edit Post");
		cut.Markup.Should().Contain("Existing title");
		cut.FindComponent<TextEditor>().Instance.Content.Should().Be("Existing content");
	}

	[Fact]
	public void EditPostShowsMarkdownContentLabel()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var postId = Guid.NewGuid();
		var post = new BlogPostDto(postId, "Test Post", "Some content", string.Empty, "Alice", string.Empty, [], DateTime.UtcNow, null, false, null);

		sender.Send(Arg.Any<GetBlogPostByIdQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<BlogPostDto?>(post)));

		Services.AddSingleton(sender);

		// Act
		var cut = RenderWithUser<Edit>(CreatePrincipal("Alice", ["Author"]), parameters => parameters.Add(p => p.Id, postId));

		// Assert — label text and Markdown hint must be present (UX parity with Create page)
		cut.Markup.Should().Contain("Content");
		cut.Markup.Should().Contain("(Markdown)");
	}

	[Fact]
	public void EditPostUsesPrimaryAndSecondaryButtonVariants()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var postId = Guid.NewGuid();
		var post = new BlogPostDto(postId, "Existing title", "Existing content", string.Empty, "Alice", string.Empty, [], DateTime.UtcNow, null, false, null);

		sender.Send(Arg.Any<GetBlogPostByIdQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<BlogPostDto?>(post)));

		Services.AddSingleton(sender);

		// Act
		var cut = RenderWithUser<Edit>(CreatePrincipal("Alice", ["Author"]), parameters => parameters.Add(p => p.Id, postId));
		var saveButton = cut.Find("button[type='submit']");
		var cancelLink = cut.Find("a[href='/blog']");

		// Assert
		saveButton.GetAttribute("class").Should().Contain("btn-primary");
		cancelLink.GetAttribute("class").Should().Contain("btn-secondary");
	}

	[Fact]
	public void EditPostShowsConcurrencyMessageWhenSaveFailsWithConcurrency()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var postId = Guid.NewGuid();
		var post = new BlogPostDto(postId, "Existing title", "Existing content", string.Empty, "Alice", string.Empty, [], DateTime.UtcNow, null, false, null);

		sender.Send(Arg.Any<GetBlogPostByIdQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<BlogPostDto?>(post)));
		sender.Send(Arg.Any<EditBlogPostCommand>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Fail("Concurrency error", ResultErrorCode.Concurrency)));

		Services.AddSingleton(sender);

		// Act
		var cut = RenderWithUser<Edit>(CreatePrincipal("Alice", ["Author"]), parameters => parameters.Add(p => p.Id, postId));

		cut.Find("form").Submit();

		// Assert
		cut.WaitForAssertion(() => cut.Markup.Should().Contain("Concurrency Conflict"));
	}

	[Fact]
	public void EditPostSubmitsAndNavigatesToBlogWhenSaveSucceeds()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var postId = Guid.NewGuid();
		var post = new BlogPostDto(postId, "Existing title", "Existing content", string.Empty, "Alice", string.Empty, [], DateTime.UtcNow, null, false, null);

		sender.Send(Arg.Any<GetBlogPostByIdQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<BlogPostDto?>(post)));
		sender.Send(Arg.Any<EditBlogPostCommand>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok()));

		Services.AddSingleton(sender);

		var navigation = Services.GetRequiredService<NavigationManager>();

		// Act
		var cut = RenderWithUser<Edit>(CreatePrincipal("Alice", ["Author"]), parameters => parameters.Add(p => p.Id, postId));

		cut.Find("form").Submit();

		// Assert
		sender.Received(1).Send(Arg.Is<EditBlogPostCommand>(command => command.Id == postId), Arg.Any<CancellationToken>());
		navigation.Uri.Should().EndWith("/blog");
	}

	[Fact]
	public void ManageRolesRendersUsersAndAvailableRoles()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var users = new[]
		{
						new UserWithRolesDto("user-1", "admin@example.com", "Admin User", ["Admin"])
				};
		var roles = new[]
		{
						new RoleDto("role-admin", "Admin"),
						new RoleDto("role-author", "Author")
				};

		sender.Send(Arg.Any<GetUsersWithRolesQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<IReadOnlyList<UserWithRolesDto>>(users)));
		sender.Send(Arg.Any<GetAvailableRolesQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<IReadOnlyList<RoleDto>>(roles)));

		Services.AddSingleton(sender);

		// Act
		var cut = RenderWithUser<ManageRoles>(CreatePrincipal("Admin User", ["Admin"]));

		// Assert
		var heading = cut.Find("header h1");

		heading.TextContent.Trim().Should().Be("Manage User Roles");
		heading.GetAttribute("class").Should().Contain("text-primary-900").And.Contain("dark:text-primary-50");
		cut.Markup.Should().Contain("Available roles: Admin, Author");
		cut.Markup.Should().Contain("admin@example.com");
	}

	[Fact]
	public void ManageRolesShowsLoadingMessageWhileQueriesArePending()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var usersTask = new TaskCompletionSource<Result<IReadOnlyList<UserWithRolesDto>>>(TaskCreationOptions.RunContinuationsAsynchronously);
		var rolesTask = new TaskCompletionSource<Result<IReadOnlyList<RoleDto>>>(TaskCreationOptions.RunContinuationsAsynchronously);

		sender.Send(Arg.Any<GetUsersWithRolesQuery>(), Arg.Any<CancellationToken>())
				.Returns(usersTask.Task);
		sender.Send(Arg.Any<GetAvailableRolesQuery>(), Arg.Any<CancellationToken>())
				.Returns(rolesTask.Task);

		Services.AddSingleton(sender);

		// Act
		var cut = RenderWithUser<ManageRoles>(CreatePrincipal("Admin User", ["Admin"]));

		// Assert
		cut.Markup.Should().Contain("Loading users...");

		usersTask.SetResult(Result.Ok<IReadOnlyList<UserWithRolesDto>>(Array.Empty<UserWithRolesDto>()));
		rolesTask.SetResult(Result.Ok<IReadOnlyList<RoleDto>>(Array.Empty<RoleDto>()));
		cut.WaitForAssertion(() => cut.Markup.Should().Contain("Actions"));
	}

	[Fact]
	public void ManageRolesShowsDismissibleErrorWhenUsersCannotLoad()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		sender.Send(Arg.Any<GetUsersWithRolesQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Fail<IReadOnlyList<UserWithRolesDto>>("Unable to load users.")));
		sender.Send(Arg.Any<GetAvailableRolesQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<IReadOnlyList<RoleDto>>(Array.Empty<RoleDto>())));

		Services.AddSingleton(sender);

		// Act
		var cut = RenderWithUser<ManageRoles>(CreatePrincipal("Admin User", ["Admin"]));

		// Assert
		cut.Markup.Should().Contain("Unable to load users.");
		cut.Find("button.alert-dismiss").Click();
		cut.Markup.Should().NotContain("Unable to load users.");
	}

	[Fact]
	public void ManageRolesAssignButtonSendsCommandAndRefreshesUsers()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var users = new[]
		{
						new UserWithRolesDto("user-1", "admin@example.com", "Admin User", ["Admin"])
				};
		var refreshedUsers = new[]
		{
						new UserWithRolesDto("user-1", "admin@example.com", "Admin User", ["Admin", "Author"])
				};
		var roles = new[]
		{
						new RoleDto("role-admin", "Admin"),
						new RoleDto("role-author", "Author")
				};

		sender.Send(Arg.Any<GetUsersWithRolesQuery>(), Arg.Any<CancellationToken>())
				.Returns(
						Task.FromResult(Result.Ok<IReadOnlyList<UserWithRolesDto>>(users)),
						Task.FromResult(Result.Ok<IReadOnlyList<UserWithRolesDto>>(refreshedUsers)));
		sender.Send(Arg.Any<GetAvailableRolesQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<IReadOnlyList<RoleDto>>(roles)));
		sender.Send(Arg.Any<AssignRoleCommand>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok()));

		Services.AddSingleton(sender);

		// Act
		var cut = RenderWithUser<ManageRoles>(CreatePrincipal("Admin User", ["Admin"]));

		cut.FindAll("button").First(button => button.TextContent.Contains("+ Author")).Click();

		// Assert
		sender.Received(1).Send(Arg.Is<AssignRoleCommand>(command => command.UserId == "user-1" && command.RoleId == "role-author"), Arg.Any<CancellationToken>());
		cut.WaitForAssertion(() => cut.Markup.Should().Contain("Admin, Author"));
	}

	[Fact]
	public void ManageRolesRemoveButtonSendsCommandAndRefreshesUsers()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var users = new[]
		{
						new UserWithRolesDto("user-1", "admin@example.com", "Admin User", ["Admin", "Author"])
				};
		var refreshedUsers = new[]
		{
						new UserWithRolesDto("user-1", "admin@example.com", "Admin User", ["Admin"])
				};
		var roles = new[]
		{
						new RoleDto("role-admin", "Admin"),
						new RoleDto("role-author", "Author")
				};

		sender.Send(Arg.Any<GetUsersWithRolesQuery>(), Arg.Any<CancellationToken>())
				.Returns(
						Task.FromResult(Result.Ok<IReadOnlyList<UserWithRolesDto>>(users)),
						Task.FromResult(Result.Ok<IReadOnlyList<UserWithRolesDto>>(refreshedUsers)));
		sender.Send(Arg.Any<GetAvailableRolesQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<IReadOnlyList<RoleDto>>(roles)));
		sender.Send(Arg.Any<RemoveRoleCommand>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok()));

		Services.AddSingleton(sender);

		// Act
		var cut = RenderWithUser<ManageRoles>(CreatePrincipal("Admin User", ["Admin"]));

		cut.FindAll("button").First(button => button.TextContent.Contains("- Author")).Click();

		// Assert
		sender.Received(1).Send(Arg.Is<RemoveRoleCommand>(command => command.UserId == "user-1" && command.RoleId == "role-author"), Arg.Any<CancellationToken>());
		cut.WaitForAssertion(() =>
		{
			cut.Markup.Should().Contain("+ Author");
			cut.Markup.Should().Contain("<td>Admin</td>");
		});
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

	private static ClaimsPrincipal CreatePrincipal(string name, string[] roles)
	{
		var claims = new List<Claim> { new(ClaimTypes.Name, name) };
		claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
		return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, ClaimTypes.Role));
	}
}
