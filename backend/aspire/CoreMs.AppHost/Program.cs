var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter("postgres-password", secret: true);
var secrets = builder.Configuration.GetSection("Secrets");

var postgres = builder.AddPostgres("postgres", password: postgresPassword, port: 5432)
    .WithDataVolume("corems-postgres-data")
    .WithPgAdmin()
    .AddDatabase("corems");

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithDataVolume("corems-rabbitmq-data")
    .WithManagementPlugin();

var communicationMs = builder.AddProject<Projects.CoreMs_CommunicationMs_Api>("communication-ms")
    .WithReference(postgres)
    .WithReference(rabbitmq)
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WithEnvironment("Jwt__SecretKey", secrets["JwtSecretKey"] ?? "")
    .WithEnvironment("Jwt__Issuer", "http://localhost:5100")
    .WithEnvironment("Queue__Enabled", "true")
    .WithEnvironment("Mail__Password", secrets["MailPassword"] ?? "");

var userMs = builder.AddProject<Projects.CoreMs_UserMs_Api>("user-ms")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithEnvironment("Jwt__SecretKey", secrets["JwtSecretKey"] ?? "")
    .WithEnvironment("TokenProvider__SecretKey", secrets["TokenProviderSecretKey"] ?? "")
    .WithEnvironment("TokenProvider__PrivateKeyBase64", secrets["TokenProviderPrivateKeyBase64"] ?? "")
    .WithEnvironment("TokenProvider__PublicKeyBase64", secrets["TokenProviderPublicKeyBase64"] ?? "")
    .WithEnvironment("CommunicationMs__BaseUrl", communicationMs.GetEndpoint("http"))
    .WithEnvironment("SocialAuth__Google__ClientId", secrets["GoogleClientId"] ?? "")
    .WithEnvironment("SocialAuth__Google__ClientSecret", secrets["GoogleClientSecret"] ?? "")
    .WithEnvironment("SocialAuth__GitHub__ClientId", secrets["GitHubClientId"] ?? "")
    .WithEnvironment("SocialAuth__GitHub__ClientSecret", secrets["GitHubClientSecret"] ?? "")
    .WithEnvironment("SocialAuth__LinkedIn__ClientId", secrets["LinkedInClientId"] ?? "")
    .WithEnvironment("SocialAuth__LinkedIn__ClientSecret", secrets["LinkedInClientSecret"] ?? "");

var documentMs = builder.AddProject<Projects.CoreMs_DocumentMs_Api>("document-ms")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithEnvironment("Jwt__SecretKey", secrets["JwtSecretKey"] ?? "")
    .WithEnvironment("Jwt__Issuer", "http://localhost:5100")
    .WithEnvironment("Storage__AccessKey", secrets["MinioAccessKey"] ?? "minioadmin")
    .WithEnvironment("Storage__SecretKey", secrets["MinioSecretKey"] ?? "minioadmin")
    .WithEnvironment("Document__LinkSigningKey", secrets["DocumentLinkSigningKey"] ?? "");

var frontend = builder.AddViteApp("frontend", "../../../frontend")
    .WithHttpEndpoint(port: 8080, env: "PORT")
    .WithEnvironment("REACT_USER_MS_BASE_URL", userMs.GetEndpoint("http"))
    .WithEnvironment("REACT_COMMUNICATION_MS_BASE_URL", communicationMs.GetEndpoint("http"))
    .WithEnvironment("REACT_DOCUMENT_MS_BASE_URL", documentMs.GetEndpoint("http"))
    .WithExternalHttpEndpoints();

builder.Build().Run();
