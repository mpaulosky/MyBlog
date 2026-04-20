//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     E2ECollection.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  E2E.Tests
//=======================================================

namespace MyBlog.E2E.Tests;

[CollectionDefinition("E2EIntegration")]
public class E2ECollection : ICollectionFixture<E2EFixture>;
