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

using Ganss.Xss;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

using MyBlog.Domain.Behaviors;
using MyBlog.ServiceDefaults;
using MyBlog.Web.Components;
using MyBlog.Web.Security;

var builder = WebApplication.CreateBuilder(args);

// AppHost browser tests run the web app under the custom Testing environment.
// Static web assets are wired automatically for Development, but not for arbitrary
// environments launched from build output. Opting in here keeps Blazor framework
// scripts, scoped CSS bundles, and component module scripts available during
// AppHost/Playwright runs without changing published behavior.
if (builder.Environment.IsEnvironment("Testing"))
{
	builder.WebHost.UseStaticWebAssets();
}

builder.AddServiceDefaults();

// Razor + Blazor Server
builder.Services.AddRazorComponents()
		.AddInteractiveServerComponents();

// Auth0 authentication — real credentials required outside the Testing environment.
var auth0Domain = builder.Configuration["Auth0:Domain"];
var auth0ClientId = builder.Configuration["Auth0:ClientId"];
var auth0ClientSecret = builder.Configuration["Auth0:ClientSecret"];
var isTestingEnvironment = builder.Environment.IsEnvironment("Testing");
var usesLocalTestLoginFallback = Auth0ConfigurationHelper.ShouldUseLocalTestLogin(
	isTestingEnvironment,
	auth0Domain,
	auth0ClientId,
	auth0ClientSecret);

// The local cookie-based test login exists only for the Testing environment that powers
// AppHost and browser automation. Development and Production must use real Auth0 settings.
if (!isTestingEnvironment)
{
	if (Auth0ConfigurationHelper.UsesPlaceholderWebAppLogin(auth0Domain, auth0ClientId, auth0ClientSecret))
	{
		throw new InvalidOperationException(
				"Auth0 configuration is missing or using placeholder values. The Web project requires real Auth0 credentials outside the Testing environment. Set these user secrets for the Web project:\n" +
				"  dotnet user-secrets set \"Auth0:Domain\" \"<your-tenant>.auth0.com\" --project src/Web\n" +
				"  dotnet user-secrets set \"Auth0:ClientId\" \"<your-client-id>\" --project src/Web\n" +
				"  dotnet user-secrets set \"Auth0:ClientSecret\" \"<your-client-secret>\" --project src/Web");
	}
}
else if (usesLocalTestLoginFallback)
{
	// Testing without real Auth0 credentials: use placeholder values and flag so the
	// AppHost and Playwright test harness can short-circuit to the local cookie login.
	auth0Domain = "test.auth0.com";
	auth0ClientId = "test-client-id";
	auth0ClientSecret = "test-client-secret";
}

var resolvedAuth0Domain = auth0Domain ?? throw new InvalidOperationException("Auth0 domain was not resolved.");
var resolvedAuth0ClientId = auth0ClientId ?? throw new InvalidOperationException("Auth0 client ID was not resolved.");
var resolvedAuth0ClientSecret = auth0ClientSecret ?? throw new InvalidOperationException("Auth0 client secret was not resolved.");

var auth0RoleClaimTypes = RoleClaimsHelper.GetRoleClaimTypes(builder.Configuration);

builder.Services.AddAuth0WebAppAuthentication(opts =>
{
	opts.Domain = resolvedAuth0Domain;
	opts.ClientId = resolvedAuth0ClientId;
	opts.ClientSecret = resolvedAuth0ClientSecret;
	opts.Scope = "openid profile email";
	opts.CallbackPath = "/signin-auth0";
});

builder.Services.PostConfigure<OpenIdConnectOptions>(Auth0Constants.AuthenticationScheme, options =>
{
	options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;

	var existingOnTokenValidated = options.Events.OnTokenValidated;
	options.Events.OnTokenValidated = async context =>
	{
		if (existingOnTokenValidated is not null)
		{
			await existingOnTokenValidated(context).ConfigureAwait(false);
		}

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

// UserManagement two-tier cache service (L1 30s + L2 2min)
builder.Services.AddUserManagementCaching();

// Repository: concrete + interface
builder.Services.AddScoped<MongoDbBlogPostRepository>();
builder.Services.AddScoped<IBlogPostRepository>(sp =>
		sp.GetRequiredService<MongoDbBlogPostRepository>());

builder.Services.AddScoped<MongoDbCategoryRepository>();
builder.Services.AddScoped<ICategoryRepository>(sp =>
		sp.GetRequiredService<MongoDbCategoryRepository>());

// MediatR — scans Web assembly for all handlers
builder.Services.AddMediatR(cfg =>
{
	cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

// FluentValidation — scans Web assembly for all validators
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// HtmlSanitizer — singleton: thread-safe, shared across requests
builder.Services.AddSingleton<IHtmlSanitizer, HtmlSanitizer>();

// Register ValidationBehavior pipeline
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// File storage (local disk — baseline for image uploads in the markdown editor)
builder.Services.AddFileStorage();

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

	// Only the Testing environment may bypass Auth0 and use the local cookie-based test login.
	if (usesLocalTestLoginFallback && app.Environment.IsEnvironment("Testing"))
	{
		ctx.Response.Redirect($"/test/login?returnUrl={Uri.EscapeDataString(safeReturn)}");
		return;
	}

	var props = new LoginAuthenticationPropertiesBuilder()
			.WithRedirectUri(safeReturn)
			.Build();
	await ctx.ChallengeAsync(Auth0Constants.AuthenticationScheme, props).ConfigureAwait(false);
}).AllowAnonymous();

app.MapGet("/Account/Logout", async ctx =>
{
	var props = new LogoutAuthenticationPropertiesBuilder()
			.WithRedirectUri("/")
			.Build();
	await ctx.SignOutAsync(Auth0Constants.AuthenticationScheme, props).ConfigureAwait(false);
	await ctx.SignOutAsync().ConfigureAwait(false);
}).RequireAuthorization();

// Test-only login endpoint for E2E testing (Testing environment only)
if (app.Environment.IsEnvironment("Testing"))
{
	app.MapGet("/test/login", MapTestLoginEndpoint).AllowAnonymous();
}

app.MapRazorComponents<App>()
		.AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();
app.Run();

[ExcludeFromCodeCoverage(Justification = "Test-only endpoint for E2E testing")]
static async Task MapTestLoginEndpoint(HttpContext ctx, string? role, string? returnUrl)
{
	var roleValue = string.IsNullOrWhiteSpace(role) ? "user" : role;
	var safeReturn = !string.IsNullOrEmpty(returnUrl)
			&& Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
			? returnUrl
			: "/";

	// Create claims for the test user
	var claims = new List<Claim>
	{
		new(ClaimTypes.NameIdentifier, "test-user-id"),
		new(ClaimTypes.Name, "Test User"),
		new(ClaimTypes.Email, "test@example.com"),
		new(ClaimTypes.Role, roleValue),
	};

	var identity = new ClaimsIdentity(claims, "TestScheme");
	var principal = new ClaimsPrincipal(identity);

	// Sign in with cookie-based authentication
	await ctx.SignInAsync("Cookies", principal, new AuthenticationProperties
	{
		IsPersistent = true,
	}).ConfigureAwait(false);

	ctx.Response.Redirect(safeReturn);
}

// Exclude the compiler-generated Program class (top-level bootstrap statements) from coverage.
[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "WebApplicationFactory<Program> requires a public entry point for integration tests.")]
[ExcludeFromCodeCoverage(Justification = "Application bootstrap entry-point — not business logic")]
public partial class Program { }
