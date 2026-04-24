//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     MongoDbFixture.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Integration.Tests
//=======================================================

using Testcontainers.MongoDb;

namespace Web.Infrastructure;

public sealed class MongoDbFixture : IAsyncLifetime
{
	private readonly MongoDbContainer _container =
#pragma warning disable CS0618
		new MongoDbBuilder().Build();
#pragma warning restore CS0618

	private string ConnectionString { get; set; } = string.Empty;

	public async Task InitializeAsync()
	{
		await _container.StartAsync();
		ConnectionString = _container.GetConnectionString();
	}

	public async Task DisposeAsync()
	{
		await _container.DisposeAsync();
	}

	internal IDbContextFactory<BlogDbContext> CreateFactory(string dbName) =>
		new TestContextFactory(ConnectionString, dbName);

	private sealed class TestContextFactory(string connectionString, string dbName)
		: IDbContextFactory<BlogDbContext>
	{
		public BlogDbContext CreateDbContext()
		{
			var options = new DbContextOptionsBuilder<BlogDbContext>()
				.UseMongoDB(connectionString, dbName)
				.Options;
			return new BlogDbContext(options);
		}

		public Task<BlogDbContext> CreateDbContextAsync(CancellationToken ct = default) =>
			Task.FromResult(CreateDbContext());
	}
}
