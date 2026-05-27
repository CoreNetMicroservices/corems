---
inclusion: fileMatch
fileMatchPattern: "**/{Controllers,Contracts}/**/*.cs"
---

# API Contracts & Code Generation (.NET)

## Approach: Contract-First with Interfaces

In .NET, instead of OpenAPI codegen (Java approach), we use:
1. **Shared DTO contracts** in `CoreMs.Common.Api` or service-specific contract projects
2. **Controller interfaces** (optional) for type safety
3. **Swagger/OpenAPI generation** from code (Swashbuckle or NSwag)

## Controller Pattern

```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

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
        var result = await _userService.GetUsersAsync(parameters, ct);
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
        var result = await _userService.GetUserByUuidAsync(userId, ct);
        return Ok(result);
    }
}
```

## DTO Conventions

### Request DTOs
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

### Response DTOs
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
- Use `required` keyword for mandatory fields
- Use `init` accessors (not `set`)
- Use nullable reference types for optional fields
- Place DTOs in the Domain layer (or a shared Contracts project)

## Validation

Use FluentValidation for request validation:

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
builder.Services.AddValidatorsFromAssemblyContaining<SignUpRequestValidator>();
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

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
});
```

## Client Generation (for service-to-service)

For generating typed clients from other services' Swagger:

```bash
# Using NSwag CLI
nswag openapi2csclient /input:http://localhost:5004/swagger/v1/swagger.json /output:TemplateMsClient.cs /namespace:CoreMs.CommunicationMs.Infrastructure.Clients
```

Or use Refit for declarative HTTP clients:
```csharp
public interface ITemplateMsApi
{
    [Post("/api/templates/render")]
    Task<RenderedTemplateResponse> RenderTemplateAsync([Body] RenderTemplateRequest request);
}

// Registration
builder.Services.AddRefitClient<ITemplateMsApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(config["ServiceUrls:TemplateMs"]!));
```
