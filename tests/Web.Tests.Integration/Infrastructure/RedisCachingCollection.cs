//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     RedisCachingCollection.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Integration.Tests
//=======================================================

namespace Web.Infrastructure;

[CollectionDefinition("RedisCaching")]
public sealed class RedisCachingCollection
: ICollectionFixture<RedisFixture>
{
}
