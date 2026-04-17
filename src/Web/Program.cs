using Auth0.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using MyBlog.Domain.Interfaces;
using MyBlog.Web.Components;
using MyBlog.Web.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Razor + Blazor Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Auth0 authentication
var auth0Domain = builder.Configuration["Auth0:Domain"];
var auth0ClientId = builder.Configuration["Auth0:ClientId"];

if (string.IsNullOrEmpty(auth0Domain) || string.IsNullOrEmpty(auth0ClientId))
{
    throw new InvalidOperationException(
        "Auth0 configuration is missing or incomplete. Set these user secrets for the Web project:\n" +
        "  dotnet user-secrets set \"Auth0:Domain\" \"<your-tenant>.auth0.com\" --project src/Web\n" +
        "  dotnet user-secrets set \"Auth0:ClientId\" \"<your-client-id>\" --project src/Web\n" +
        "  dotnet user-secrets set \"Auth0:ClientSecret\" \"<your-client-secret>\" --project src/Web");
}

builder.Services.AddAuth0WebAppAuthentication(opts =>
{
    opts.Domain = auth0Domain;
    opts.ClientId = auth0ClientId;
    opts.ClientSecret = builder.Configuration["Auth0:ClientSecret"];
    opts.CallbackPath = "/signin-auth0";
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

// Repository: concrete + interface
builder.Services.AddScoped<MongoDbBlogPostRepository>();
builder.Services.AddScoped<IBlogPostRepository>(sp =>
    sp.GetRequiredService<MongoDbBlogPostRepository>());

// MediatR — scans Web assembly for all handlers
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

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
    var props = new LoginAuthenticationPropertiesBuilder()
        .WithRedirectUri(returnUrl ?? "/")
        .Build();
    await ctx.ChallengeAsync(Auth0Constants.AuthenticationScheme, props);
}).AllowAnonymous();

app.MapGet("/Account/Logout", async (HttpContext ctx) =>
{
    var props = new LogoutAuthenticationPropertiesBuilder()
        .WithRedirectUri("/")
        .Build();
    await ctx.SignOutAsync(Auth0Constants.AuthenticationScheme, props);
    await ctx.SignOutAsync();
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();
app.Run();

