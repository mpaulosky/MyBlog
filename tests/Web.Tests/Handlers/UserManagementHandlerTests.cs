//Project Name :  Web.Tests
//=======================================================

using System.Net;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Configuration;

using MyBlog.Web.Features.UserManagement;

namespace Web.Handlers;

public class UserManagementHandlerTests
{
	private const string InvalidAccessTokenError = "Auth0 Management API token response did not contain a valid access_token.";

	private readonly IConfiguration _config = Substitute.For<IConfiguration>();
	private readonly IHttpClientFactory _httpFactory = Substitute.For<IHttpClientFactory>();
	private readonly IUserManagementCacheService _cache;
	private readonly UserManagementHandler _handler;

	public UserManagementHandlerTests()
	{
		_config["Auth0:ManagementApiDomain"].Returns((string?)null);
		_cache = BuildPassThroughCache();
		_handler = new UserManagementHandler(_config, _httpFactory, _cache);
	}

	// ── Domain missing ──────────────────────────────────────────────────────────────

	[Fact]
	public async Task HandleGetUsersWithRolesDomainMissingReturnsFailResult()
	{
		// Arrange (none)

		// Act
		var result = await _handler.Handle(new GetUsersWithRolesQuery(), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiDomain not configured");
	}

	[Fact]
	public async Task HandleAssignRoleDomainMissingReturnsFailResult()
	{
		// Arrange (none)

		// Act
		var result = await _handler.Handle(
			new AssignRoleCommand("user-1", "role-1"), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiDomain not configured");
	}

	[Fact]
	public async Task HandleRemoveRoleDomainMissingReturnsFailResult()
	{
		// Arrange (none)

		// Act
		var result = await _handler.Handle(
			new RemoveRoleCommand("user-1", "role-1"), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiDomain not configured");
	}

	[Fact]
	public async Task HandleGetAvailableRolesDomainMissingReturnsFailResult()
	{
		// Arrange (none)

		// Act
		var result = await _handler.Handle(new GetAvailableRolesQuery(), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiDomain not configured");
	}

	// ── ClientId missing ────────────────────────────────────────────────────────────────

	[Fact]
	public async Task HandleGetUsersWithRolesClientIdMissingReturnsFailResult()
	{
		// Arrange
		var handler = BuildHandlerClientIdMissing();

		// Act
		var result = await handler.Handle(new GetUsersWithRolesQuery(), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiClientId not configured");
	}

	[Fact]
	public async Task HandleAssignRoleClientIdMissingReturnsFailResult()
	{
		// Arrange
		var handler = BuildHandlerClientIdMissing();

		// Act
		var result = await handler.Handle(
			new AssignRoleCommand("user-1", "role-1"), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiClientId not configured");
	}

	[Fact]
	public async Task HandleRemoveRoleClientIdMissingReturnsFailResult()
	{
		// Arrange
		var handler = BuildHandlerClientIdMissing();

		// Act
		var result = await handler.Handle(
			new RemoveRoleCommand("user-1", "role-1"), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiClientId not configured");
	}

	[Fact]
	public async Task HandleGetAvailableRolesClientIdMissingReturnsFailResult()
	{
		// Arrange
		var handler = BuildHandlerClientIdMissing();

		// Act
		var result = await handler.Handle(new GetAvailableRolesQuery(), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiClientId not configured");
	}

	// ── ClientSecret missing ──────────────────────────────────────────────────────────────

	[Fact]
	public async Task HandleGetUsersWithRolesClientSecretMissingReturnsFailResult()
	{
		// Arrange
		var handler = BuildHandlerClientSecretMissing();

		// Act
		var result = await handler.Handle(new GetUsersWithRolesQuery(), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiClientSecret not configured");
	}

	[Fact]
	public async Task HandleAssignRoleClientSecretMissingReturnsFailResult()
	{
		// Arrange
		var handler = BuildHandlerClientSecretMissing();

		// Act
		var result = await handler.Handle(
			new AssignRoleCommand("user-1", "role-1"), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiClientSecret not configured");
	}

	[Fact]
	public async Task HandleRemoveRoleClientSecretMissingReturnsFailResult()
	{
		// Arrange
		var handler = BuildHandlerClientSecretMissing();

		// Act
		var result = await handler.Handle(
			new RemoveRoleCommand("user-1", "role-1"), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiClientSecret not configured");
	}

	[Fact]
	public async Task HandleGetAvailableRolesClientSecretMissingReturnsFailResult()
	{
		// Arrange
		var handler = BuildHandlerClientSecretMissing();

		// Act
		var result = await handler.Handle(new GetAvailableRolesQuery(), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiClientSecret not configured");
	}

	// ── HTTP token endpoint fails ────────────────────────────────────────────────────────────────────

	[Fact]
	public async Task HandleGetUsersWithRolesTokenEndpointFailsReturnsFailResult()
	{
		// Arrange
		using var httpHandler = new StubHttpHandler(HttpStatusCode.InternalServerError);
		using var httpClient = new HttpClient(httpHandler);
		var handler = BuildHandlerHttpFail(new StaticHttpClientFactory(httpClient));

		// Act
		var result = await handler.Handle(new GetUsersWithRolesQuery(), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("500");
	}

	[Fact]
	public async Task HandleAssignRoleTokenEndpointFailsReturnsFailResult()
	{
		// Arrange
		using var httpHandler = new StubHttpHandler(HttpStatusCode.InternalServerError);
		using var httpClient = new HttpClient(httpHandler);
		var handler = BuildHandlerHttpFail(new StaticHttpClientFactory(httpClient));

		// Act
		var result = await handler.Handle(
			new AssignRoleCommand("user-1", "role-1"), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("500");
	}

	[Fact]
	public async Task HandleRemoveRoleTokenEndpointFailsReturnsFailResult()
	{
		// Arrange
		using var httpHandler = new StubHttpHandler(HttpStatusCode.InternalServerError);
		using var httpClient = new HttpClient(httpHandler);
		var handler = BuildHandlerHttpFail(new StaticHttpClientFactory(httpClient));

		// Act
		var result = await handler.Handle(
			new RemoveRoleCommand("user-1", "role-1"), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("500");
	}

	[Fact]
	public async Task HandleGetAvailableRolesTokenEndpointFailsReturnsFailResult()
	{
		// Arrange
		using var httpHandler = new StubHttpHandler(HttpStatusCode.InternalServerError);
		using var httpClient = new HttpClient(httpHandler);
		var handler = BuildHandlerHttpFail(new StaticHttpClientFactory(httpClient));

		// Act
		var result = await handler.Handle(new GetAvailableRolesQuery(), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("500");
	}

	// ── Management configuration/token contract ──────────────────────────────────────────────

	[Fact]
	public async Task HandleGetAvailableRolesPrimaryManagementKeysUsePrimaryConfigAndConfiguredAudience()
	{
		// Arrange
		using var httpHandler = new RecordingTokenHttpHandler("{\"access_token\":\"   \"}");
		using var httpClient = new HttpClient(httpHandler, disposeHandler: false);
		var handler = BuildHandlerWithPrimaryKeys(new StaticHttpClientFactory(httpClient), "https://api.example.com/");

		// Act
		var result = await handler.Handle(new GetAvailableRolesQuery(), CancellationToken.None);
		httpHandler.LastRequestBody.Should().NotBeNullOrWhiteSpace();
		using var requestBody = JsonDocument.Parse(httpHandler.LastRequestBody!);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Be(InvalidAccessTokenError);
		httpHandler.LastRequestUri.Should().Be(new Uri("https://primary.auth0.com/oauth/token"));
		requestBody.RootElement.GetProperty("client_id").GetString().Should().Be("primary-client-id");
		requestBody.RootElement.GetProperty("client_secret").GetString().Should().Be("primary-client-secret");
		requestBody.RootElement.GetProperty("audience").GetString().Should().Be("https://api.example.com/");
		requestBody.RootElement.GetProperty("grant_type").GetString().Should().Be("client_credentials");
	}

	[Fact]
	public async Task HandleGetAvailableRolesWhitespacePrimaryManagementKeysFallBackToLegacyConfig()
	{
		// Arrange
		using var httpHandler = new RecordingTokenHttpHandler("{\"access_token\":\"\"}");
		using var httpClient = new HttpClient(httpHandler, disposeHandler: false);
		var handler = BuildHandlerWithLegacyFallback(new StaticHttpClientFactory(httpClient));

		// Act
		var result = await handler.Handle(new GetAvailableRolesQuery(), CancellationToken.None);
		httpHandler.LastRequestBody.Should().NotBeNullOrWhiteSpace();
		using var requestBody = JsonDocument.Parse(httpHandler.LastRequestBody!);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Be(InvalidAccessTokenError);
		httpHandler.LastRequestUri.Should().Be(new Uri("https://legacy.auth0.com/oauth/token"));
		requestBody.RootElement.GetProperty("client_id").GetString().Should().Be("legacy-client-id");
		requestBody.RootElement.GetProperty("client_secret").GetString().Should().Be("legacy-client-secret");
		requestBody.RootElement.GetProperty("audience").GetString().Should().Be("https://legacy.auth0.com/api/v2/");
		requestBody.RootElement.GetProperty("grant_type").GetString().Should().Be("client_credentials");
	}

	[Fact]
	public async Task HandleGetAvailableRolesNestedAuth0ManagementKeysFallBackToNestedConfig()
	{
		// Arrange
		using var httpHandler = new RecordingTokenHttpHandler("{\"access_token\":\"\"}");
		using var httpClient = new HttpClient(httpHandler, disposeHandler: false);
		var handler = BuildHandlerWithNestedAuth0ManagementKeys(new StaticHttpClientFactory(httpClient));

		// Act
		var result = await handler.Handle(new GetAvailableRolesQuery(), CancellationToken.None);
		httpHandler.LastRequestBody.Should().NotBeNullOrWhiteSpace();
		using var requestBody = JsonDocument.Parse(httpHandler.LastRequestBody!);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Be(InvalidAccessTokenError);
		httpHandler.LastRequestUri.Should().Be(new Uri("https://nested.auth0.com/oauth/token"));
		requestBody.RootElement.GetProperty("client_id").GetString().Should().Be("nested-client-id");
		requestBody.RootElement.GetProperty("client_secret").GetString().Should().Be("nested-client-secret");
		requestBody.RootElement.GetProperty("audience").GetString().Should().Be("https://nested.auth0.com/api/v2/");
		requestBody.RootElement.GetProperty("grant_type").GetString().Should().Be("client_credentials");
	}

	[Theory]
	[InlineData("{\"access_token\":\"\"}")]
	[InlineData("{\"access_token\":\"   \"}")]
	[InlineData("{}")]
	public async Task HandleGetAvailableRolesBlankOrMissingAccessTokenReturnsExplicitFailure(string tokenResponseJson)
	{
		// Arrange
		using var httpHandler = new RecordingTokenHttpHandler(tokenResponseJson);
		using var httpClient = new HttpClient(httpHandler, disposeHandler: false);
		var handler = BuildHandlerWithPrimaryKeys(new StaticHttpClientFactory(httpClient), "https://api.example.com/");

		// Act
		var result = await handler.Handle(new GetAvailableRolesQuery(), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Be(InvalidAccessTokenError);
	}

	// ── helpers ───────────────────────────────────────────────────────────────────────────────

	private static IUserManagementCacheService BuildPassThroughCache()
	{
		var cache = Substitute.For<IUserManagementCacheService>();
		cache.GetOrFetchUsersAsync(
				Arg.Any<Func<Task<IReadOnlyList<UserWithRolesDto>>>>(),
				Arg.Any<CancellationToken>())
			.Returns(ci => new ValueTask<IReadOnlyList<UserWithRolesDto>>(
				ci.Arg<Func<Task<IReadOnlyList<UserWithRolesDto>>>>()()));
		cache.GetOrFetchRolesAsync(
				Arg.Any<Func<Task<IReadOnlyList<RoleDto>>>>(),
				Arg.Any<CancellationToken>())
			.Returns(ci => new ValueTask<IReadOnlyList<RoleDto>>(
				ci.Arg<Func<Task<IReadOnlyList<RoleDto>>>>()()));
		return cache;
	}

	private static UserManagementHandler BuildHandlerWithPrimaryKeys(IHttpClientFactory httpFactory, string audience)
	{
		var config = Substitute.For<IConfiguration>();
		config["Auth0Management:Domain"].Returns("primary.auth0.com");
		config["Auth0Management:ClientId"].Returns("primary-client-id");
		config["Auth0Management:ClientSecret"].Returns("primary-client-secret");
		config["Auth0Management:Audience"].Returns(audience);
		config["Auth0:ManagementApiDomain"].Returns("legacy.auth0.com");
		config["Auth0:ManagementApiClientId"].Returns("legacy-client-id");
		config["Auth0:ManagementApiClientSecret"].Returns("legacy-client-secret");
		return new UserManagementHandler(config, httpFactory, BuildPassThroughCache());
	}

	private static UserManagementHandler BuildHandlerWithLegacyFallback(IHttpClientFactory httpFactory)
	{
		var config = Substitute.For<IConfiguration>();
		config["Auth0Management:Domain"].Returns("   ");
		config["Auth0Management:ClientId"].Returns("\t");
		config["Auth0Management:ClientSecret"].Returns(" ");
		config["Auth0Management:Audience"].Returns(" ");
		config["Auth0:ManagementApiDomain"].Returns("legacy.auth0.com");
		config["Auth0:ManagementApiClientId"].Returns("legacy-client-id");
		config["Auth0:ManagementApiClientSecret"].Returns("legacy-client-secret");
		return new UserManagementHandler(config, httpFactory, BuildPassThroughCache());
	}

	private static UserManagementHandler BuildHandlerWithNestedAuth0ManagementKeys(IHttpClientFactory httpFactory)
	{
		var config = Substitute.For<IConfiguration>();
		config["Auth0:Auth0Management:Domain"].Returns("nested.auth0.com");
		config["Auth0:Auth0Management:ClientId"].Returns("nested-client-id");
		config["Auth0:Auth0Management:ClientSecret"].Returns("nested-client-secret");
		return new UserManagementHandler(config, httpFactory, BuildPassThroughCache());
	}

	private static UserManagementHandler BuildHandlerClientIdMissing()
	{
		var config = Substitute.For<IConfiguration>();
		config["Auth0:ManagementApiDomain"].Returns("test.auth0.com");
		config["Auth0:ManagementApiClientId"].Returns((string?)null);
		return new UserManagementHandler(config, Substitute.For<IHttpClientFactory>(), BuildPassThroughCache());
	}

	private static UserManagementHandler BuildHandlerClientSecretMissing()
	{
		var config = Substitute.For<IConfiguration>();
		config["Auth0:ManagementApiDomain"].Returns("test.auth0.com");
		config["Auth0:ManagementApiClientId"].Returns("test-client-id");
		config["Auth0:ManagementApiClientSecret"].Returns((string?)null);
		return new UserManagementHandler(config, Substitute.For<IHttpClientFactory>(), BuildPassThroughCache());
	}

	private static UserManagementHandler BuildHandlerHttpFail(IHttpClientFactory httpFactory)
	{
		var config = Substitute.For<IConfiguration>();
		config["Auth0:ManagementApiDomain"].Returns("test.auth0.com");
		config["Auth0:ManagementApiClientId"].Returns("test-client-id");
		config["Auth0:ManagementApiClientSecret"].Returns("test-client-secret");
		return new UserManagementHandler(config, httpFactory, BuildPassThroughCache());
	}

	private sealed class StubHttpHandler(HttpStatusCode statusCode) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(new HttpResponseMessage(statusCode));
	}

	private sealed class StaticHttpClientFactory(HttpClient httpClient) : IHttpClientFactory
	{
		public HttpClient CreateClient(string name) => httpClient;
	}

	private sealed class RecordingTokenHttpHandler(string responseJson) : HttpMessageHandler
	{
		public Uri? LastRequestUri { get; private set; }

		public string? LastRequestBody { get; private set; }

		protected override async Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request, CancellationToken cancellationToken)
		{
			LastRequestUri = request.RequestUri;
			LastRequestBody = request.Content is null
				? null
				: await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
			};
		}
	}
}

