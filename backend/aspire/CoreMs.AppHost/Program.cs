var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter("postgres-password", secret: true);
var secrets = builder.Configuration.GetSection("Secrets");

var postgres = builder.AddPostgres("postgres", password: postgresPassword, port: 5432)
    .WithDataVolume("corems-postgres-data")
    .WithPgAdmin()
    .AddDatabase("corems");

var userMs = builder.AddProject<Projects.CoreMs_UserMs_Api>("user-ms")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithEnvironment("Jwt__SecretKey", secrets["JwtSecretKey"] ?? "")
    .WithEnvironment("RabbitMq__Password", secrets["RabbitMqPassword"] ?? "guest")
    .WithEnvironment("SocialAuth__Google__ClientId", secrets["GoogleClientId"] ?? "")
    .WithEnvironment("SocialAuth__Google__ClientSecret", secrets["GoogleClientSecret"] ?? "")
    .WithEnvironment("SocialAuth__GitHub__ClientId", secrets["GitHubClientId"] ?? "")
    .WithEnvironment("SocialAuth__GitHub__ClientSecret", secrets["GitHubClientSecret"] ?? "")
    .WithEnvironment("SocialAuth__LinkedIn__ClientId", secrets["LinkedInClientId"] ?? "")
    .WithEnvironment("SocialAuth__LinkedIn__ClientSecret", secrets["LinkedInClientSecret"] ?? "");

var communicationMs = builder.AddProject<Projects.CoreMs_CommunicationMs_Api>("communication-ms")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithEnvironment("Jwt__SecretKey", secrets["JwtSecretKey"] ?? "")
    .WithEnvironment("Jwt__Issuer", "http://localhost:5100")
    .WithEnvironment("Mail__Password", secrets["MailPassword"] ?? "");

var frontend = builder.AddViteApp("frontend", "../../../frontend")
    .WithHttpEndpoint(port: 8080, env: "PORT")
    .WithEnvironment("REACT_USER_MS_BASE_URL", userMs.GetEndpoint("http"))
    .WithEnvironment("REACT_COMMUNICATION_MS_BASE_URL", communicationMs.GetEndpoint("http"))
    .WithExternalHttpEndpoints();

builder.Build().Run();
