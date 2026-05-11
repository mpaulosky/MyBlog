//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     EditAclTests.cs
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
using MyBlog.Web.Features.BlogPosts.Edit;

using Web.Testing;

namespace Web.Features;

public class EditAclTests : BunitContext
{
	private readonly TestAuthenticationStateProvider _authProvider = new();

	public EditAclTests()
	{
		Services.AddAuthorizationCore();
		Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();
		Services.AddSingleton<AuthenticationStateProvider>(_authProvider);
	}

	[Fact]
	public void EditRedirectsToBlogWhenPostNotFound()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var postId = Guid.NewGuid();

		sender.Send(Arg.Any<GetBlogPostByIdQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<BlogPostDto?>(null)));

		Services.AddSingleton(sender);

		var navigation = Services.GetRequiredService<NavigationManager>();

		// Act
		RenderWithUser<Edit>(
				CreatePrincipalWithSub("auth0|some-user", ["Author"]),
				parameters => parameters.Add(p => p.Id, postId));

		// Assert
		navigation.Uri.Should().EndWith("/blog");
	}

	[Fact]
	public void EditRedirectsToBlogWhenAuthorIsNotPostOwner()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var postId = Guid.NewGuid();
		const string OwnerSub = "auth0|owner-user";
		const string NonOwnerSub = "auth0|other-user";
		var post = new BlogPostDto(postId, "Test Post", "Content", OwnerSub, "Owner", string.Empty, [], DateTime.UtcNow, null, false);

		sender.Send(Arg.Any<GetBlogPostByIdQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<BlogPostDto?>(post)));

		Services.AddSingleton(sender);

		var navigation = Services.GetRequiredService<NavigationManager>();

		// Act
		RenderWithUser<Edit>(
				CreatePrincipalWithSub(NonOwnerSub, ["Author"]),
				parameters => parameters.Add(p => p.Id, postId));

		// Assert
		navigation.Uri.Should().EndWith("/blog");
	}

	[Fact]
	public void EditAllowsAccessWhenAuthorIsPostOwner()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var postId = Guid.NewGuid();
		const string OwnerSub = "auth0|owner-user";
		var post = new BlogPostDto(postId, "Test Post", "Content", OwnerSub, "Owner", string.Empty, [], DateTime.UtcNow, null, false);

		sender.Send(Arg.Any<GetBlogPostByIdQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<BlogPostDto?>(post)));

		Services.AddSingleton(sender);

		var navigation = Services.GetRequiredService<NavigationManager>();

		// Act
		var cut = RenderWithUser<Edit>(
				CreatePrincipalWithSub(OwnerSub, ["Author"]),
				parameters => parameters.Add(p => p.Id, postId));

		// Assert
		navigation.Uri.Should().NotEndWith("/blog");
		cut.Markup.Should().Contain("Edit Post");
	}

	[Fact]
	public void EditShowsErrorWhenServerReturnsUnauthorized()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var postId = Guid.NewGuid();
		const string OwnerSub = "auth0|owner-user";
		var post = new BlogPostDto(postId, "Test Post", "Content", OwnerSub, "Owner", string.Empty, [], DateTime.UtcNow, null, false);

		sender.Send(Arg.Any<GetBlogPostByIdQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<BlogPostDto?>(post)));

		sender.Send(Arg.Any<EditBlogPostCommand>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Fail("You are not authorized to edit this post.", ResultErrorCode.Unauthorized)));

		Services.AddSingleton(sender);

		var navigation = Services.GetRequiredService<NavigationManager>();

		var cut = RenderWithUser<Edit>(
				CreatePrincipalWithSub(OwnerSub, ["Author"]),
				parameters => parameters.Add(p => p.Id, postId));

		// Act
		cut.Find("button[type='submit']").Click();

		// Assert
		cut.WaitForAssertion(() =>
				cut.Markup.Should().Contain("You don't have permission to edit this post."));
		navigation.Uri.Should().NotEndWith("/blog");
	}

	[Fact]
	public void EditAllowsAdminToEditAnyPost()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var postId = Guid.NewGuid();
		const string OwnerSub = "auth0|some-author";
		const string AdminSub = "auth0|admin-user";
		var post = new BlogPostDto(postId, "Test Post", "Content", OwnerSub, "SomeAuthor", string.Empty, [], DateTime.UtcNow, null, false);

		sender.Send(Arg.Any<GetBlogPostByIdQuery>(), Arg.Any<CancellationToken>())
				.Returns(Task.FromResult(Result.Ok<BlogPostDto?>(post)));

		Services.AddSingleton(sender);

		var navigation = Services.GetRequiredService<NavigationManager>();

		// Act
		var cut = RenderWithUser<Edit>(
				CreatePrincipalWithSub(AdminSub, ["Admin"]),
				parameters => parameters.Add(p => p.Id, postId));

		// Assert
		navigation.Uri.Should().NotEndWith("/blog");
		cut.Markup.Should().Contain("Edit Post");
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
