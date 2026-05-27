---
inclusion: always
---

# Async Patterns in CoreMS (.NET)

## Overview

CoreMS .NET uses async/await throughout for I/O-bound operations. This replaces Java's Virtual Threads approach — in .NET, the async model is built into the framework and achieves similar concurrency benefits.

## Why Async/Await?

CoreMS microservices are I/O-heavy:
- **Database operations** (EF Core — all async by default)
- **Service-to-service HTTP calls** (HttpClient)
- **Message queue operations** (MassTransit/RabbitMQ)
- **File storage operations** (MinIO S3)
- **External API calls** (OAuth, email providers)

Async/await allows handling thousands of concurrent requests without blocking threads.

## Rules

### ✅ DO: Use Async for All I/O

```csharp
public async Task<UserEntity?> GetByEmailAsync(string email, CancellationToken ct = default)
{
    return await _context.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
}
```

### ✅ DO: Pass CancellationToken Everywhere

```csharp
[HttpGet("{userId:guid}")]
public async Task<ActionResult<UserInfoDto>> GetUser(Guid userId, CancellationToken ct)
{
    var user = await _userService.GetUserByUuidAsync(userId, ct);
    return Ok(user);
}
```

### ✅ DO: Use ConfigureAwait(false) in Library Code

```csharp
// In CoreMs.Common or Infrastructure layers (not in Controllers)
public async Task<UserEntity?> GetByEmailAsync(string email, CancellationToken ct = default)
{
    return await _context.Users
        .FirstOrDefaultAsync(u => u.Email == email, ct)
        .ConfigureAwait(false);
}
```

### ❌ DON'T: Block on Async Code

```csharp
// WRONG - causes deadlocks
var user = _userService.GetUserByUuidAsync(userId).Result;
var user = _userService.GetUserByUuidAsync(userId).GetAwaiter().GetResult();

// CORRECT
var user = await _userService.GetUserByUuidAsync(userId, ct);
```

### ❌ DON'T: Use async void

```csharp
// WRONG - exceptions are unobservable
public async void SendEmail(string to, string body) { }

// CORRECT
public async Task SendEmailAsync(string to, string body, CancellationToken ct = default) { }
```

### ✅ DO: Use Task.WhenAll for Parallel I/O

```csharp
public async Task<DashboardDto> GetDashboardAsync(Guid userId, CancellationToken ct)
{
    var userTask = _userService.GetUserByUuidAsync(userId, ct);
    var docsTask = _documentClient.GetUserDocumentsAsync(userId, ct);
    var notifsTask = _communicationClient.GetNotificationsAsync(userId, ct);

    await Task.WhenAll(userTask, docsTask, notifsTask);

    return new DashboardDto
    {
        User = await userTask,
        Documents = await docsTask,
        Notifications = await notifsTask
    };
}
```

## Naming Convention

- All async methods MUST end with `Async` suffix
- ✅ `GetUserByUuidAsync`, `SendEmailAsync`, `RenderTemplateAsync`
- ❌ `GetUserByUuid`, `SendEmail`, `RenderTemplate`

## Performance Expectations

### .NET Async vs Java Virtual Threads
Both achieve similar goals (high concurrency for I/O-bound work):
- .NET: Compiler-generated state machines, thread pool reuse
- Java: Lightweight JVM-managed threads

### Typical Behavior
- Thousands of concurrent requests handled efficiently
- Thread pool threads released during I/O waits
- Minimal memory overhead per request
- Graceful scaling under load

## Background Tasks

Use `IHostedService` or `BackgroundService` for long-running background work:

```csharp
public class TokenCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ILoginTokenRepository>();
            await repo.DeleteExpiredTokensAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
```

## Messaging (MassTransit)

```csharp
public class SendEmailConsumer : IConsumer<SendEmailCommand>
{
    private readonly IEmailService _emailService;

    public async Task Consume(ConsumeContext<SendEmailCommand> context)
    {
        await _emailService.SendAsync(context.Message, context.CancellationToken);
    }
}
```
