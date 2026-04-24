//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     Program.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web
//=======================================================

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

using Auth0.AspNetCore.Authentication;

using FluentValidation;

using MediatR;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;

using MyBlog.Domain.Behaviors;
using MyBlog.Domain.Entities;
using MyBlog.ServiceDefaults;
using MyBlog.Web.Components;
using MyBlog.Web.Infrastructure.Caching;
using MyBlog.Web.Security;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Razor + Blazor Server
builder.Services.AddRazorComponents()
		.AddInteractiveServerComponents();

// Auth0 authentication — required only in Production
var auth0Domain = builder.Configuration["Auth0:Domain"];
var auth0ClientId = builder.Configuration["Auth0:ClientId"];

// In Development/Testing, provide mock values; in Production, require real credentials
if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
{
	if (string.IsNullOrEmpty(auth0Domain) || string.IsNullOrEmpty(auth0ClientId))
	{
		throw new InvalidOperationException(
				"Auth0 configuration is missing or incomplete. Set these user secrets for the Web project:\n" +
				"  dotnet user-secrets set \"Auth0:Domain\" \"<your-tenant>.auth0.com\" --project src/Web\n" +
				"  dotnet user-secrets set \"Auth0:ClientId\" \"<your-client-id>\" --project src/Web\n" +
				"  dotnet user-secrets set \"Auth0:ClientSecret\" \"<your-client-secret>\" --project src/Web");
	}
}
else if (string.IsNullOrWhiteSpace(auth0Domain) || string.IsNullOrWhiteSpace(auth0ClientId))
{
	// Development/Testing: Use test/mock values if not configured
	auth0Domain = "test.auth0.com";
	auth0ClientId = "test-client-id";
}

var auth0RoleClaimTypes = RoleClaimsHelper.GetRoleClaimTypes(builder.Configuration);

builder.Services.AddAuth0WebAppAuthentication(opts =>
{
	opts.Domain = auth0Domain;
	opts.ClientId = auth0ClientId;
	opts.ClientSecret = builder.Configuration["Auth0:ClientSecret"];
	opts.CallbackPath = "/signin-auth0";
});

builder.Services.PostConfigure<OpenIdConnectOptions>(Auth0Constants.AuthenticationScheme, options =>
{
	options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;

	var existingOnTokenValidated = options.Events.OnTokenValidated;
	options.Events.OnTokenValidated = async context =>
	{
		if (existingOnTokenValidated is not null)
			await existingOnTokenValidated(context);

		if (context.Principal?.Identity is not ClaimsIdentity identity)
		{
			return;
		}

		RoleClaimsHelper.AddRoleClaims(identity, auth0RoleClaimTypes);
	};
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();

// MongoDB (Aspire-managed connection "myblog")
builder.AddMongoDBClient("myblog");
builder.Services.AddDbContextFactory<BlogDbContext>((sp, options) =>
{
	var mongoClient = sp.GetRequiredService<MongoDB.Driver.IMongoClient>();
	options.UseMongoDB(mongoClient, "myblog");
});

// Redis distributed cache (Aspire-managed connection "redis")
builder.AddRedisDistributedCache("redis");

// Local in-memory cache (L1)
builder.Services.AddMemoryCache();

// BlogPost two-tier cache service (L1 + L2)
builder.Services.AddBlogPostCaching();

// Repository: concrete + interface
builder.Services.AddScoped<MongoDbBlogPostRepository>();
builder.Services.AddScoped<IBlogPostRepository>(sp =>
		sp.GetRequiredService<MongoDbBlogPostRepository>());

// MediatR — scans Web assembly for all handlers
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

// FluentValidation — scans Web assembly for all validators
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Register ValidationBehavior pipeline
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// HttpClient for Auth0 Management API
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();

// Auth0 login/logout endpoints
app.MapGet("/Account/Login", async (HttpContext ctx, string? returnUrl) =>
{
	var safeReturn = !string.IsNullOrEmpty(returnUrl)
			&& Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
			? returnUrl
			: "/";
	var props = new LoginAuthenticationPropertiesBuilder()
			.WithRedirectUri(safeReturn)
			.Build();
	await ctx.ChallengeAsync(Auth0Constants.AuthenticationScheme, props);
}).AllowAnonymous();

app.MapGet("/Account/Logout", async ctx =>
{
	var props = new LogoutAuthenticationPropertiesBuilder()
			.WithRedirectUri("/")
			.Build();
	await ctx.SignOutAsync(Auth0Constants.AuthenticationScheme, props);
	await ctx.SignOutAsync();
}).RequireAuthorization();

// Test-only login endpoint for E2E testing (Development/Testing environments only)
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
	app.MapGet("/test/login", MapTestLoginEndpoint).AllowAnonymous();
}

app.MapRazorComponents<App>()
		.AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();
app.Run();

[ExcludeFromCodeCoverage(Justification = "Test-only endpoint for E2E testing")]
static async Task MapTestLoginEndpoint(HttpContext ctx, string? role)
{
	var roleValue = string.IsNullOrWhiteSpace(role) ? "user" : role;

	// Create claims for the test user
	var claims = new List<Claim>
	{
		new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
		new Claim(ClaimTypes.Name, "Test User"),
		new Claim(ClaimTypes.Email, "test@example.com"),
		new Claim(ClaimTypes.Role, roleValue),
	};

	var identity = new ClaimsIdentity(claims, "TestScheme");
	var principal = new ClaimsPrincipal(identity);

	// Sign in with cookie-based authentication
	await ctx.SignInAsync("Cookies", principal, new AuthenticationProperties
	{
		IsPersistent = true,
	});

	ctx.Response.Redirect("/");
}

// Exclude the compiler-generated Program class (top-level bootstrap statements) from coverage.
[ExcludeFromCodeCoverage(Justification = "Application bootstrap entry-point — not business logic")]
public partial class Program { }
