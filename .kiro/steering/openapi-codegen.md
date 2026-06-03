---
inclusion: fileMatch
fileMatchPattern: "**/{Controllers,Contracts,Models}/**/*.cs"
---

# API Contracts & Controllers (.NET)

## Approach: Code-First with Swagger Generation

In .NET CoreMS, we use code-first approach:
1. **DTOs (Models)** in `CoreMs.<Service>Ms.Core/Models/`
2. **Controllers** with XML docs and ProducesResponseType attributes
3. **Swagger/OpenAPI generation** from code via Swashbuckle

## Controller Pattern

```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController(UserService userService) : ControllerBase
{
    /// <summary>
    /// Get paginated list of users (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = CoreMsRoles.UserMsAdmin)]
    [ProducesResponseType(typeof(PagedResult<UserInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<UserInfoDto>>> GetUsers(
        [FromQuery] QueryParameters parameters,
        CancellationToken ct)
    {
        var result = await userService.GetUsersAsync(parameters, ct);
        return Ok(result);
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{userId:guid}")]
    [Authorize(Roles = CoreMsRoles.UserMsAdmin)]
    [ProducesResponseType(typeof(UserInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserInfoDto>> GetUser(Guid userId, CancellationToken ct)
    {
        var result = await userService.GetUserByUuidAsync(userId, ct);
        return Ok(result);
    }
}
```

### Controller Conventions
- Use primary constructor for dependency injection
- Inject concrete service classes (no interfaces needed — they use `[Service]` attribute)
- Use `CancellationToken ct` as last parameter on all async actions
- Use `[FromQuery]` for query parameters, route constraints for path params

## DTO Conventions

### Request Models (in Core/Models/)
```csharp
public record SignUpRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhoneNumber { get; init; }
}
```

### Response Models (in Core/Models/)
```csharp
public record UserInfoDto
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}
```

### Rules
- Use `record` types for DTOs (immutable, value equality)
- Use `required` keyword for mandatory request fields
- Use `init` accessors (not `set`)
- Use nullable reference types for optional fields
- Place all models in `CoreMs.<Service>Ms.Core/Models/`

## Validation (FluentValidation)

Validators live in `CoreMs.<Service>Ms.Api/Validators/`:

```csharp
public class SignUpRequestValidator : AbstractValidator<SignUpRequest>
{
    public SignUpRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Must contain uppercase letter")
            .Matches("[a-z]").WithMessage("Must contain lowercase letter")
            .Matches("[0-9]").WithMessage("Must contain digit");
    }
}
```

Register validators:
```csharp
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
```

### ValidationFilter

A custom action filter in `Api/Filters/` runs FluentValidation before controller actions:

```csharp
// Registered in AddControllers options
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});
```

## Swagger Configuration

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "User Management Service",
        Version = "v1",
        Description = "OAuth2/OIDC Authorization Server with user management"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
});
```

## Client Generation (for service-to-service)

For generating typed clients from other services' Swagger:

```bash
# Using NSwag CLI
nswag openapi2csclient /input:http://localhost:5104/swagger/v1/swagger.json /output:TemplateMsClient.cs /namespace:CoreMs.CommunicationMs.Core.Clients
```

Or use Refit for declarative HTTP clients:
```csharp
public interface ITemplateMsApi
{
    [Post("/api/templates/render")]
    Task<RenderedTemplateResponse> RenderTemplateAsync([Body] RenderTemplateRequest request);
}

builder.Services.AddRefitClient<ITemplateMsApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(config["ServiceUrls:TemplateMs"]!));
```
