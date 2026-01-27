var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithHostPort(5488)
    .WithDataVolume("todo-postgres-data");

var tododb = postgres.AddDatabase("tododb");

var cache = builder.AddRedis("cache");

builder.AddProject<Projects.Todo_Api>("api")
    .WithReference(tododb)
    .WithReference(cache)
    .WaitFor(tododb)
    .WaitFor(cache);

builder.Build().Run();
