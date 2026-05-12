//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     RichTextEditorTests.cs
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
using MyBlog.Web.Features.BlogPosts.Create;
using MyBlog.Web.Features.BlogPosts.Edit;

using Web.Testing;

namespace Web.Features;

public class RichTextEditorTests : BunitContext
{
	private readonly TestAuthenticationStateProvider _authProvider = new();

	public RichTextEditorTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddAuthorizationCore();
		Services.AddSingleton<IAuthorizationService, TestAuthorizationService>();
		Services.AddSingleton<AuthenticationStateProvider>(_authProvider);
	}

	[Fact]
	public void EditorRendersOnCreatePage()
	{
		// Arrange
		Services.AddSingleton(Substitute.For<ISender>());
		_authProvider.SetUser(CreatePrincipal("Alice", ["Author"]));

		// Act
		var cut = Render<Create>(parameters =>
			parameters.AddCascadingValue(
				Task.FromResult(new AuthenticationState(CreatePrincipal("Alice", ["Author"])))));

		// Assert
		cut.FindComponent<RichTextBlazorfied.RTBlazorfied>();
	}

	[Fact]
	public void EditorRendersOnEditPage()
	{
		// Arrange
		var sender = Substitute.For<ISender>();
		var postId = Guid.NewGuid();
		var post = new BlogPostDto(postId, "Test title", "<p>Test content</p>", string.Empty, "Alice", string.Empty, [], DateTime.UtcNow, null, false);

		sender.Send(Arg.Any<GetBlogPostByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<BlogPostDto?>(post)));

		Services.AddSingleton(sender);
		_authProvider.SetUser(CreatePrincipal("Alice", ["Author"]));

		// Act
		var cut = Render<Edit>(parameters =>
		{
			parameters.AddCascadingValue(
				Task.FromResult(new AuthenticationState(CreatePrincipal("Alice", ["Author"]))));
			parameters.Add(p => p.Id, postId);
		});

		// Assert
		cut.FindComponent<RichTextBlazorfied.RTBlazorfied>();
	}

	private static ClaimsPrincipal CreatePrincipal(string name, string[] roles)
	{
		var claims = new List<Claim> { new(ClaimTypes.Name, name) };
		claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
		return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, ClaimTypes.Role));
	}
}
