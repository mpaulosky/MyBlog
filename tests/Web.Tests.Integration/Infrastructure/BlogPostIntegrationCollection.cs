//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     BlogPostIntegrationCollection.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Integration.Tests
//=======================================================

namespace Web.Infrastructure;

[CollectionDefinition("BlogPostIntegration")]
public sealed class BlogPostIntegrationCollection
	: ICollectionFixture<MongoDbFixture>
{
}
