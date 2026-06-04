var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .AddDatabase("corems");

var userMs = builder.AddProject<Projects.CoreMs_UserMs_Api>("user-ms")
    .WithReference(postgres)
    .WaitFor(postgres);

var frontend = builder.AddViteApp("frontend", "../../frontend")
    .WithHttpEndpoint(port: 8080, env: "PORT")
    .WithEnvironment("REACT_USER_MS_BASE_URL", userMs.GetEndpoint("http"))
    .WithExternalHttpEndpoints();

builder.Build().Run();
