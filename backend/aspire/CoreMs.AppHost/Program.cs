var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter("postgres-password", secret: true);
var common = builder.Configuration.GetSection("Common");
var userMs_ = builder.Configuration.GetSection("UserMs");
var communicationMs_ = builder.Configuration.GetSection("CommunicationMs");
var documentMs_ = builder.Configuration.GetSection("DocumentMs");

// --- Infrastructure ---

var postgres = builder.AddPostgres("postgres", password: postgresPassword, port: 5432)
    .WithDataVolume("corems-postgres-data")
    .WithPgAdmin();

var corems = postgres.AddDatabase("corems");

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

// --- Services ---

var templateMs = builder.AddProject<Projects.CoreMs_TemplateMs_Api>("template-ms")
    .WithReference(corems)
    .WaitFor(corems)
    .WithEnvironment("Jwt__SecretKey", common["JwtSecretKey"] ?? "")
    .WithEnvironment("Jwt__Issuer", "http://localhost:5100");

var documentMs = builder.AddProject<Projects.CoreMs_DocumentMs_Api>("document-ms")
    .WithReference(corems)
    .WithReference(templateMs)
    .WaitFor(corems)
    .WaitFor(minio)
    .WaitFor(templateMs)
    .WithEnvironment("Jwt__SecretKey", common["JwtSecretKey"] ?? "")
    .WithEnvironment("Jwt__Issuer", "http://localhost:5100")
    .WithEnvironment("Storage__Endpoint", minio.GetEndpoint("api"))
    .WithEnvironment("Storage__AccessKey", minioAccessKey)
    .WithEnvironment("Storage__SecretKey", minioSecretKey)
    .WithEnvironment("Document__LinkSigningKey", documentMs_["DocumentLinkSigningKey"] ?? "");

var communicationMs = builder.AddProject<Projects.CoreMs_CommunicationMs_Api>("communication-ms")
    .WithReference(corems)
    .WithReference(rabbitmq)
    .WithReference(templateMs)
    .WithReference(documentMs)
    .WaitFor(corems)
    .WaitFor(rabbitmq)
    .WaitFor(templateMs)
    .WaitFor(documentMs)
    .WithEnvironment("Jwt__SecretKey", common["JwtSecretKey"] ?? "")
    .WithEnvironment("Jwt__Issuer", "http://localhost:5100")
    .WithEnvironment("Queue__Enabled", "true")
    .WithEnvironment("Mail__Enabled", communicationMs_["MailEnabled"] ?? "false")
    .WithEnvironment("Mail__Host", communicationMs_["MailHost"] ?? "localhost")
    .WithEnvironment("Mail__Port", communicationMs_["MailPort"] ?? "1025")
    .WithEnvironment("Mail__Username", communicationMs_["MailUsername"] ?? "")
    .WithEnvironment("Mail__Password", communicationMs_["MailPassword"] ?? "")
    .WithEnvironment("Mail__DefaultFrom", communicationMs_["MailDefaultFrom"] ?? "noreply@corems.local")
    .WithEnvironment("Mail__UseSsl", communicationMs_["MailUseSsl"] ?? "false");

var userMs = builder.AddProject<Projects.CoreMs_UserMs_Api>("user-ms")
    .WithReference(corems)
    .WaitFor(corems)
    .WithEnvironment("Jwt__SecretKey", common["JwtSecretKey"] ?? "")
    .WithEnvironment("Jwt__PrivateKeyBase64", userMs_["JwtPrivateKeyBase64"] ?? "")
    .WithEnvironment("Jwt__PublicKeyBase64", userMs_["JwtPublicKeyBase64"] ?? "")
    .WithEnvironment("CommunicationMs__BaseUrl", communicationMs.GetEndpoint("http"))
    .WithEnvironment("SocialAuth__Google__ClientId", userMs_["GoogleClientId"] ?? "")
    .WithEnvironment("SocialAuth__Google__ClientSecret", userMs_["GoogleClientSecret"] ?? "")
    .WithEnvironment("SocialAuth__GitHub__ClientId", userMs_["GitHubClientId"] ?? "")
    .WithEnvironment("SocialAuth__GitHub__ClientSecret", userMs_["GitHubClientSecret"] ?? "")
    .WithEnvironment("SocialAuth__LinkedIn__ClientId", userMs_["LinkedInClientId"] ?? "")
    .WithEnvironment("SocialAuth__LinkedIn__ClientSecret", userMs_["LinkedInClientSecret"] ?? "");

var translationMs = builder.AddProject<Projects.CoreMs_TranslationMs_Api>("translation-ms")
    .WithReference(corems)
    .WaitFor(corems)
    .WithEnvironment("Jwt__SecretKey", common["JwtSecretKey"] ?? "")
    .WithEnvironment("Jwt__Issuer", "http://localhost:5100");

// --- Frontend ---

builder.AddViteApp("frontend", "../../../frontend")
    .WithHttpEndpoint(port: 8080, env: "PORT")
    .WithEnvironment("REACT_USER_MS_BASE_URL", userMs.GetEndpoint("http"))
    .WithEnvironment("REACT_COMMUNICATION_MS_BASE_URL", communicationMs.GetEndpoint("http"))
    .WithEnvironment("REACT_DOCUMENT_MS_BASE_URL", documentMs.GetEndpoint("http"))
    .WithEnvironment("REACT_TRANSLATION_MS_BASE_URL", translationMs.GetEndpoint("http"))
    .WithEnvironment("REACT_TEMPLATE_MS_BASE_URL", templateMs.GetEndpoint("http"))
    .WithExternalHttpEndpoints();

builder.Build().Run();
