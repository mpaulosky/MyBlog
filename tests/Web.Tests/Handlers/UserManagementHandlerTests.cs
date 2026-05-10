//Project Name :  Web.Tests
//=======================================================

using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Configuration;

using MyBlog.Web.Features.UserManagement;

namespace Unit.Handlers;

public class UserManagementHandlerTests
{
	private readonly IConfiguration _config = Substitute.For<IConfiguration>();
	private readonly IHttpClientFactory _httpFactory = Substitute.For<IHttpClientFactory>();
	private readonly UserManagementHandler _handler;

	public UserManagementHandlerTests()
	{
		_config["Auth0:ManagementApiDomain"].Returns((string?)null);
		_handler = new UserManagementHandler(_config, _httpFactory);
	}

	// ── Domain missing ──────────────────────────────────────────────────────────────

	[Fact]
	public async Task Handle_GetUsersWithRoles_DomainMissing_ReturnsFailResult()
	{
		// Arrange (none)

		// Act
		var result = await _handler.Handle(new GetUsersWithRolesQuery(), CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Auth0:ManagementApiDomain not configured");
	}

	[Fact]
	public async Task Handle_AssignRole_DomainMissing_ReturnsFailResult()
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
	public async Task Handle_RemoveRole_DomainMissing_ReturnsFailResult()
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
	public async Task Handle_GetAvailableRoles_DomainMissing_ReturnsFailResult()
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
	public async Task Handle_GetUsersWithRoles_ClientIdMissing_ReturnsFailResult()
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
	public async Task Handle_AssignRole_ClientIdMissing_ReturnsFailResult()
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
	public async Task Handle_RemoveRole_ClientIdMissing_ReturnsFailResult()
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
	public async Task Handle_GetAvailableRoles_ClientIdMissing_ReturnsFailResult()
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
	public async Task Handle_GetUsersWithRoles_ClientSecretMissing_ReturnsFailResult()
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
	public async Task Handle_AssignRole_ClientSecretMissing_ReturnsFailResult()
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
	public async Task Handle_RemoveRole_ClientSecretMissing_ReturnsFailResult()
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
	public async Task Handle_GetAvailableRoles_ClientSecretMissing_ReturnsFailResult()
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
	public async Task Handle_GetUsersWithRoles_TokenEndpointFails_ReturnsFailResult()
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
	public async Task Handle_AssignRole_TokenEndpointFails_ReturnsFailResult()
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
	public async Task Handle_RemoveRole_TokenEndpointFails_ReturnsFailResult()
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
	public async Task Handle_GetAvailableRoles_TokenEndpointFails_ReturnsFailResult()
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

	// ── Management configuration/token success path ─────────────────────────────────────────

	[Fact]
	public async Task GetManagementClientAsyncUsesPrimaryAuth0ManagementKeysAndConfiguredAudience()
	{
		// Arrange
		using var httpHandler = new RecordingTokenHttpHandler("{\"access_token\":\"primary-token\"}");
		using var httpClient = new HttpClient(httpHandler, disposeHandler: false);
		var handler = BuildHandlerWithPrimaryKeys(new StaticHttpClientFactory(httpClient), "https://api.example.com/");

		// Act
		var client = await InvokeGetManagementClientAsync(handler);
		httpHandler.LastRequestBody.Should().NotBeNullOrWhiteSpace();
		using var requestBody = JsonDocument.Parse(httpHandler.LastRequestBody!);

		// Assert
		client.GetType().FullName.Should().Be("Auth0.ManagementApi.ManagementApiClient");
		httpHandler.LastRequestUri.Should().Be(new Uri("https://primary.auth0.com/oauth/token"));
		requestBody.RootElement.GetProperty("client_id").GetString().Should().Be("primary-client-id");
		requestBody.RootElement.GetProperty("client_secret").GetString().Should().Be("primary-client-secret");
		requestBody.RootElement.GetProperty("audience").GetString().Should().Be("https://api.example.com/");
		requestBody.RootElement.GetProperty("grant_type").GetString().Should().Be("client_credentials");
	}

	[Fact]
	public async Task GetManagementClientAsyncPrimaryWhitespaceFallsBackToLegacyKeysAndDefaultAudience()
	{
		// Arrange
		using var httpHandler = new RecordingTokenHttpHandler("{\"access_token\":\"legacy-token\"}");
		using var httpClient = new HttpClient(httpHandler, disposeHandler: false);
		var handler = BuildHandlerWithLegacyFallback(new StaticHttpClientFactory(httpClient));

		// Act
		var client = await InvokeGetManagementClientAsync(handler);
		httpHandler.LastRequestBody.Should().NotBeNullOrWhiteSpace();
		using var requestBody = JsonDocument.Parse(httpHandler.LastRequestBody!);

		// Assert
		client.GetType().FullName.Should().Be("Auth0.ManagementApi.ManagementApiClient");
		httpHandler.LastRequestUri.Should().Be(new Uri("https://legacy.auth0.com/oauth/token"));
		requestBody.RootElement.GetProperty("client_id").GetString().Should().Be("legacy-client-id");
		requestBody.RootElement.GetProperty("client_secret").GetString().Should().Be("legacy-client-secret");
		requestBody.RootElement.GetProperty("audience").GetString().Should().Be("https://legacy.auth0.com/api/v2/");
		requestBody.RootElement.GetProperty("grant_type").GetString().Should().Be("client_credentials");
	}

	[Fact]
	public void TokenResponseDeserializesAccessTokenFromAuth0SnakeCasePayload()
	{
		// Arrange
		var tokenResponseType = typeof(UserManagementHandler).GetNestedType("TokenResponse", BindingFlags.NonPublic);
		tokenResponseType.Should().NotBeNull();
		if (tokenResponseType is null)
		{
			throw new InvalidOperationException("TokenResponse type was not found.");
		}

		// Act
		var tokenData = JsonSerializer.Deserialize("{\"access_token\":\"abc123\"}", tokenResponseType);
		var accessToken = tokenResponseType
			.GetProperty("AccessToken", BindingFlags.Instance | BindingFlags.Public)
			?.GetValue(tokenData) as string;

		// Assert
		tokenData.Should().NotBeNull();
		accessToken.Should().Be("abc123");
	}

	// ── helpers ───────────────────────────────────────────────────────────────────────────────

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
		return new UserManagementHandler(config, httpFactory);
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
		return new UserManagementHandler(config, httpFactory);
	}

	private static async Task<object> InvokeGetManagementClientAsync(UserManagementHandler handler)
	{
		var method = typeof(UserManagementHandler).GetMethod("GetManagementClientAsync", BindingFlags.Instance | BindingFlags.NonPublic);
		method.Should().NotBeNull();
		if (method is null)
		{
			throw new InvalidOperationException("GetManagementClientAsync was not found.");
		}

		var task = method.Invoke(handler, [CancellationToken.None]) as Task;
		task.Should().NotBeNull();
		if (task is null)
		{
			throw new InvalidOperationException("GetManagementClientAsync did not return a task.");
		}

		await task.ConfigureAwait(false);

		var resultProperty = task.GetType().GetProperty("Result", BindingFlags.Instance | BindingFlags.Public);
		resultProperty.Should().NotBeNull();
		if (resultProperty is null)
		{
			throw new InvalidOperationException("GetManagementClientAsync task result property was not found.");
		}

		return resultProperty.GetValue(task)
			?? throw new InvalidOperationException("GetManagementClientAsync returned a null client.");
	}

	private static UserManagementHandler BuildHandlerClientIdMissing()
	{
		var config = Substitute.For<IConfiguration>();
		config["Auth0:ManagementApiDomain"].Returns("test.auth0.com");
		config["Auth0:ManagementApiClientId"].Returns((string?)null);
		return new UserManagementHandler(config, Substitute.For<IHttpClientFactory>());
	}

	private static UserManagementHandler BuildHandlerClientSecretMissing()
	{
		var config = Substitute.For<IConfiguration>();
		config["Auth0:ManagementApiDomain"].Returns("test.auth0.com");
		config["Auth0:ManagementApiClientId"].Returns("test-client-id");
		config["Auth0:ManagementApiClientSecret"].Returns((string?)null);
		return new UserManagementHandler(config, Substitute.For<IHttpClientFactory>());
	}

	private static UserManagementHandler BuildHandlerHttpFail(IHttpClientFactory httpFactory)
	{
		var config = Substitute.For<IConfiguration>();
		config["Auth0:ManagementApiDomain"].Returns("test.auth0.com");
		config["Auth0:ManagementApiClientId"].Returns("test-client-id");
		config["Auth0:ManagementApiClientSecret"].Returns("test-client-secret");
		return new UserManagementHandler(config, httpFactory);
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

