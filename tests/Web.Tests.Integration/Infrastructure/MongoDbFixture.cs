//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     MongoDbFixture.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests.Integration
//=======================================================

using Microsoft.EntityFrameworkCore.Diagnostics;

using Testcontainers.MongoDb;

namespace Web.Infrastructure;

public sealed class MongoDbFixture : IAsyncLifetime
{
	private readonly MongoDbContainer _container =
		new MongoDbBuilder("mongo").Build();

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

	public IDbContextFactory<BlogDbContext> CreateFactory(string dbName) =>
		new TestContextFactory(ConnectionString, dbName);

	private sealed class TestContextFactory(string connectionString, string dbName)
		: IDbContextFactory<BlogDbContext>
	{
		private readonly DbContextOptions<BlogDbContext> _options =
			new DbContextOptionsBuilder<BlogDbContext>()
				.UseMongoDB(connectionString, dbName)
				.ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
				.Options;

		public BlogDbContext CreateDbContext() => new(_options);

		public Task<BlogDbContext> CreateDbContextAsync(CancellationToken ct = default) =>
			Task.FromResult(CreateDbContext());
	}
}
