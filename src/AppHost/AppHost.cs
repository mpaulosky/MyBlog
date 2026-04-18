//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     AppHost.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  AppHost
//=======================================================

var builder = DistributedApplication.CreateBuilder(args);

var mongo = builder.AddMongoDB("mongodb");
var mongoDb = mongo.AddDatabase("myblog");
var redis = builder.AddRedis("redis");

builder.AddProject<Projects.Web>("web")
    .WithReference(mongoDb)
    .WithReference(redis)
    .WaitFor(mongo)
    .WaitFor(redis);

builder.Build().Run();
