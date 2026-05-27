var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .AddDatabase("corems");

var rabbitmq = builder.AddRabbitMQ("rabbitmq");

// TODO: Add project reference to user-ms API once created (Task 1.4)
// var userMs = builder.AddProject<Projects.CoreMs_UserMs_Api>("user-ms")
//     .WithReference(postgres)
//     .WithReference(rabbitmq);

builder.Build().Run();
