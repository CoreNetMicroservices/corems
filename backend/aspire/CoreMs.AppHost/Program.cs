var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter("postgres-password", secret: true);
var secrets = builder.Configuration.GetSection("Secrets");

var postgres = builder.AddPostgres("postgres", password: postgresPassword, port: 5432)
    .WithDataVolume("corems-postgres-data")
    .WithPgAdmin()
    .AddDatabase("corems");

var rabbitmqPassword = builder.AddParameter("rabbitmq-password", secret: true);

var rabbitmq = builder.AddRabbitMQ("rabbitmq", password: rabbitmqPassword)
    .WithDataVolume("corems-rabbitmq-data")
    .WithManagementPlugin();

var minioAccessKey = builder.AddParameter("minio-access-key", secret: true);
var minioSecretKey = builder.AddParameter("minio-secret-key", secret: true);

var minio = builder.AddContainer("minio", "minio/minio", "latest")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithEnvironment("MINIO_ROOT_USER", minioAccessKey)
    .WithEnvironment("MINIO_ROOT_PASSWORD", minioSecretKey)
    .WithHttpEndpoint(port: 9000, targetPort: 9000, name: "api")
    .WithHttpEndpoint(port: 9001, targetPort: 9001, name: "console")
    .WithVolume("corems-minio-data", "/data")
    .WithHttpHealthCheck(endpointName: "api", path: "/minio/health/live");

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
    .WithEnvironment("Jwt__PrivateKeyBase64", secrets["JwtPrivateKeyBase64"] ?? "")
    .WithEnvironment("Jwt__PublicKeyBase64", secrets["JwtPublicKeyBase64"] ?? "")
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
    .WaitFor(minio)
    .WithEnvironment("Jwt__SecretKey", secrets["JwtSecretKey"] ?? "")
    .WithEnvironment("Jwt__Issuer", "http://localhost:5100")
    .WithEnvironment("Storage__Endpoint", minio.GetEndpoint("api"))
    .WithEnvironment("Storage__AccessKey", minioAccessKey)
    .WithEnvironment("Storage__SecretKey", minioSecretKey)
    .WithEnvironment("Document__LinkSigningKey", secrets["DocumentLinkSigningKey"] ?? "");

var translationMs = builder.AddProject<Projects.CoreMs_TranslationMs_Api>("translation-ms")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WithEnvironment("Jwt__SecretKey", secrets["JwtSecretKey"] ?? "")
    .WithEnvironment("Jwt__Issuer", "http://localhost:5100");

var frontend = builder.AddViteApp("frontend", "../../../frontend")
    .WithHttpEndpoint(port: 8080, env: "PORT")
    .WithEnvironment("REACT_USER_MS_BASE_URL", userMs.GetEndpoint("http"))
    .WithEnvironment("REACT_COMMUNICATION_MS_BASE_URL", communicationMs.GetEndpoint("http"))
    .WithEnvironment("REACT_DOCUMENT_MS_BASE_URL", documentMs.GetEndpoint("http"))
    .WithEnvironment("REACT_TRANSLATION_MS_BASE_URL", translationMs.GetEndpoint("http"))
    .WithExternalHttpEndpoints();

builder.Build().Run();
