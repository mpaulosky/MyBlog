//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     CategoryIntegrationCollection.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests.Integration
//=======================================================

namespace Web.Infrastructure;

[CollectionDefinition("CategoryIntegration")]
public sealed class CategoryIntegrationCollection
	: ICollectionFixture<MongoDbFixture>
{
}
