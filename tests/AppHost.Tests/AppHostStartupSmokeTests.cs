// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     AppHostStartupSmokeTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : MyBlog
// Project Name :  AppHost.Tests
// =============================================

using AppHost.Infrastructure;

using FluentAssertions;

namespace AppHost;

/// <summary>
/// Focused startup smoke coverage for the real Aspire AppHost.
/// </summary>
[Collection("MongoClearIntegration")]
public sealed class AppHostStartupSmokeTests(ClearCommandAppFixture fixture)
{
	[SkipInCIFact]
	public async Task AppHost_Starts_Web_And_Resolves_MongoDb_Connection_String()
	{
		// Arrange
		fixture.MongoConnectionString.Should().NotBeNullOrWhiteSpace();
		fixture.MongoConnectionString.Should().Contain("mongodb://");

		var webEndpoint = fixture.App.GetEndpoint("web", "https");
		using var handler = new HttpClientHandler();
		handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

		using var client = new HttpClient(handler) { BaseAddress = webEndpoint };

		// Act
		using var response = await WaitForAliveAsync(client, TimeSpan.FromMinutes(3));

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	private static async Task<HttpResponseMessage> WaitForAliveAsync(HttpClient client, TimeSpan timeout)
	{
		client.Timeout = timeout;
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		cts.CancelAfter(timeout);

		Exception? lastError = null;
		HttpStatusCode? lastStatusCode = null;

		while (!cts.Token.IsCancellationRequested)
		{
			try
			{
				var response = await client.GetAsync(new Uri("/alive", UriKind.Relative), cts.Token);

				if (response.IsSuccessStatusCode)
				{
					return response;
				}

				lastStatusCode = response.StatusCode;
				response.Dispose();
			}
			catch (OperationCanceledException) when (!TestContext.Current.CancellationToken.IsCancellationRequested && cts.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				lastError = ex;
			}

			await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
		}

		throw new TimeoutException(
			$"AppHost web endpoint did not return success from /alive within {timeout.TotalSeconds:F0}s. Last status: {lastStatusCode?.ToString() ?? "none"}.",
			lastError);
	}
}
