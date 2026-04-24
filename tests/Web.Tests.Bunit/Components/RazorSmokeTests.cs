//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     RazorSmokeTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Unit.Tests
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
	public RazorSmokeTests()
	{
		Services.AddAuthorizationCore();
		Services.AddSingleton<IAuthorizationService>(new TestAuthorizationService());
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
						new BlogPostDto(Guid.NewGuid(), "First", "Content", "Alice", DateTime.UtcNow, null, false)
				};

		sender.Send(Arg.Any<GetBlogPostsQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<IReadOnlyList<BlogPostDto>>(posts)));

		Services.AddSingleton(sender);

		// Act
		var cut = RenderWithUser<MyBlog.Web.Features.BlogPosts.List.Index>(CreatePrincipal("Alice", ["Author"]));

		// Assert
		cut.Markup.Should().Contain("First");
		cut.Markup.Should().Contain("Edit");
		cut.Find("button").Click();
		cut.Markup.Should().Contain("Confirm Delete");
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
	public void BlogIndexConfirmDeleteSendsDeleteCommandAndRefreshesList()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var postId = Guid.NewGuid();
		var posts = new[]
		{
						new BlogPostDto(postId, "First", "Content", "Alice", DateTime.UtcNow, null, false)
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
		cut.FindAll("button").Last(button => button.TextContent.Contains("Delete", StringComparison.Ordinal)).Click();

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
						new BlogPostDto(postId, "First", "Content", "Alice", DateTime.UtcNow, null, false)
				};

		sender.Send(Arg.Any<GetBlogPostsQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<IReadOnlyList<BlogPostDto>>(posts)));
		sender.Send(Arg.Any<DeleteBlogPostCommand>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Fail("Concurrency error", ResultErrorCode.Concurrency)));

		Services.AddSingleton(sender);

		// Act
		var cut = RenderWithUser<MyBlog.Web.Features.BlogPosts.List.Index>(CreatePrincipal("Alice", ["Author"]));

		cut.Find("button").Click();
		cut.FindAll("button").Last(button => button.TextContent.Contains("Delete", StringComparison.Ordinal)).Click();

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
		cut.Markup.Should().Contain("Create Post");
		cut.FindAll("input").Count.Should().BeGreaterThanOrEqualTo(2);
		cut.Find("textarea");
	}

	[Fact]
	public void CreatePostSubmitsAndNavigatesToBlogWhenCommandSucceeds()
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
		cut.FindAll("input")[1].Change("Alice");
		cut.Find("textarea").Change("Hello world");
		cut.Find("form").Submit();

		// Assert
		sender.Received(1).Send(Arg.Is<CreateBlogPostCommand>(command =>
				command.Title == "My title" &&
				command.Author == "Alice" &&
				command.Content == "Hello world"), Arg.Any<CancellationToken>());
		navigation.Uri.Should().EndWith("/blog");
	}

	[Fact]
	public void EditPostLoadsExistingPost()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var postId = Guid.NewGuid();
		var post = new BlogPostDto(postId, "Existing title", "Existing content", "Alice", DateTime.UtcNow, null, false);

		sender.Send(Arg.Any<GetBlogPostByIdQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<BlogPostDto?>(post)));

		Services.AddSingleton(sender);

		// Act
		var cut = RenderWithUser<Edit>(CreatePrincipal("Alice", ["Author"]), parameters => parameters.Add(p => p.Id, postId));

		// Assert
		cut.Markup.Should().Contain("Edit Post");
		cut.Markup.Should().Contain("Existing title");
		cut.Markup.Should().Contain("Existing content");
	}

	[Fact]
	public void EditPostShowsConcurrencyMessageWhenSaveFailsWithConcurrency()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var postId = Guid.NewGuid();
		var post = new BlogPostDto(postId, "Existing title", "Existing content", "Alice", DateTime.UtcNow, null, false);

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
		var post = new BlogPostDto(postId, "Existing title", "Existing content", "Alice", DateTime.UtcNow, null, false);

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
		cut.Markup.Should().Contain("Manage User Roles");
		cut.Markup.Should().Contain("Available roles: Admin, Author");
		cut.Markup.Should().Contain("admin@example.com");
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

		cut.FindAll("button").First(button => button.TextContent.Contains("+ Author", StringComparison.Ordinal)).Click();

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

		cut.FindAll("button").First(button => button.TextContent.Contains("- Author", StringComparison.Ordinal)).Click();

		// Assert
		sender.Received(1).Send(Arg.Is<RemoveRoleCommand>(command => command.UserId == "user-1" && command.RoleId == "role-author"), Arg.Any<CancellationToken>());
		cut.WaitForAssertion(() =>
		{
			cut.Markup.Should().Contain("+ Author");
			cut.Markup.Should().Contain("<td class=\"px-4 py-3\">Admin</td>");
		});
	}

	private IRenderedComponent<TComponent> RenderWithUser<TComponent>(
			ClaimsPrincipal principal,
			Action<ComponentParameterCollectionBuilder<TComponent>>? configure = null)
			where TComponent : IComponent
	{
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
