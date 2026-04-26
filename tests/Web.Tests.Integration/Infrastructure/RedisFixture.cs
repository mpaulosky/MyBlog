//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     RedisFixture.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests.Integration
//=======================================================

using Microsoft.Extensions.DependencyInjection;

using Testcontainers.Redis;

namespace Web.Infrastructure;

public sealed class RedisFixture : IAsyncLifetime
{
	private readonly RedisContainer _container =
#pragma warning disable CS0618
		new RedisBuilder().Build();
#pragma warning restore CS0618

	public string ConnectionString { get; private set; } = string.Empty;

	public async ValueTask InitializeAsync()
	{
		await _container.StartAsync();
		ConnectionString = _container.GetConnectionString();
	}

	public async ValueTask DisposeAsync()
	{
		await _container.DisposeAsync();
	}

	/// <summary>
	/// Creates a fresh <see cref="IBlogPostCacheService"/> backed by a new
	/// <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> (cold L1)
	/// and the shared Redis container (L2). Each call returns an independent
	/// instance so tests can verify the L2 path by comparing behaviour across instances.
	/// </summary>
	internal IBlogPostCacheService CreateCacheService()
	{
		var services = new ServiceCollection();
		services.AddMemoryCache();
		services.AddStackExchangeRedisCache(opt => opt.Configuration = ConnectionString);
		services.AddBlogPostCaching();
		return services.BuildServiceProvider().GetRequiredService<IBlogPostCacheService>();
	}
}
