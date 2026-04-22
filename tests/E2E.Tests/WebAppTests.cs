//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     WebAppTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  E2E.Tests
//=======================================================

namespace MyBlog.E2E.Tests;

[Collection("E2EIntegration")]
public class WebAppTests(E2EFixture fixture)
{
	[Fact]
	public async Task GetHomePage_ReturnsOk()
	{
		// Arrange
		var httpClient = fixture.App.CreateHttpClient("web");
		httpClient.Timeout = TimeSpan.FromSeconds(300); // Increase timeout for CI

		// Act
		var response = await httpClient.GetAsync("/");

		// Assert
		response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
	}
}
