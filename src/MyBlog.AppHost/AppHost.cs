var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.MyBlog_Web>("web");

builder.Build().Run();
