# Implementation Plan: User Management Service C# Migration

## Overview

Migrate the User Management Service from Java Spring Boot to C# / ASP.NET Core 9 using OpenIddict for OAuth2/OIDC, Entity Framework Core for data access, MassTransit for messaging, and .NET Aspire for local orchestration. The implementation follows a phased approach: solution structure & skeleton → entities & repositories → controllers, services, and tests.

## Tasks

- [ ] 1. Solution structure, shared libraries, and Aspire skeleton
  - [x] 1.1 Create Directory.Build.props and Directory.Packages.props with central package management
    - Define shared build properties (TargetFramework net9.0, Nullable enable, ImplicitUsings enable)
    - Add all NuGet package versions to Directory.Packages.props (OpenIddict, EF Core, MassTransit, FluentValidation, BCrypt, xUnit, FsCheck, etc.)
    - _Requirements: 17.1_

  - [x] 1.2 Create CoreMs.Common library projects
    - Create `src/Common/CoreMs.Common/` with base classes, PagedResult<T>, QueryParameters
    - Create `src/Common/CoreMs.Common.Contracts/` with shared DTOs (SendEmailCommand, SendSmsCommand)
    - Create `src/Common/CoreMs.Common.Security/` with ICurrentUserService interface, CoreMsRoles constants, JWT validation helpers
    - _Requirements: 9.1, 15.1, 16.6_

  - [x] 1.3 Create Aspire orchestration projects
    - Create `src/Aspire/CoreMs.AppHost/` with Program.cs defining PostgreSQL, RabbitMQ resources and user-ms project reference
    - Create `src/Aspire/CoreMs.ServiceDefaults/` with Extensions.cs (OpenTelemetry, health checks, service discovery)
    - _Requirements: 18.1, 18.2_

  - [x] 1.4 Create user-ms project structure (Api, Domain, Infrastructure)
    - Create `src/user-ms/src/CoreMs.UserMs.Api/` project with ASP.NET Core Web API template
    - Create `src/user-ms/src/CoreMs.UserMs.Domain/` class library
    - Create `src/user-ms/src/CoreMs.UserMs.Infrastructure/` class library with EF Core references
    - Create `src/user-ms/tests/CoreMs.UserMs.Tests/` xUnit test project
    - Create `src/user-ms/tests/CoreMs.UserMs.IntegrationTests/` xUnit test project with Testcontainers
    - Set up project references (Api → Domain, Infrastructure; Infrastructure → Domain; Domain → Common)
    - _Requirements: 17.1_

  - [x] 1.5 Create CoreMs.sln referencing all projects
    - Add all projects (Common, Aspire, user-ms src and tests) to the solution file
    - Verify `dotnet build` succeeds with empty projects
    - _Requirements: 17.1_

- [x] 2. Checkpoint - Verify solution builds
  - Ensure all projects compile, ask the user if questions arise.

- [ ] 3. Domain layer — entities, enums, exceptions, and interfaces
  - [x] 3.1 Create domain entities (UserEntity, UserRoleEntity, LoginTokenEntity, ActionTokenEntity, AuthorizationCodeEntity)
    - Define all properties matching the existing PostgreSQL schema (user_ms schema)
    - Include navigation properties and collections
    - Create ActionTokenType enum (EmailVerification, PhoneVerification, PasswordReset)
    - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5_

  - [x] 3.2 Create domain exceptions
    - AuthenticationException, AccountDisabledException, EmailNotVerifiedException
    - EntityNotFoundException, DuplicateEmailException, TokenExpiredException, TokenConsumedException
    - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 13.7_

  - [x] 3.3 Create repository interfaces (IUserRepository, ILoginTokenRepository, IActionTokenRepository, IAuthorizationCodeRepository)
    - Define async methods with CancellationToken for all CRUD and query operations
    - Include PagedResult<T> return types for paginated queries
    - _Requirements: 9.3, 14.1, 14.4_

  - [x] 3.4 Create domain service interfaces (IUserService, IAuthService, ITokenService, ISocialAuthService)
    - Define all async method signatures matching the design document
    - _Requirements: 1.1, 3.1, 7.1, 8.1, 9.2, 10.1, 11.1_

- [ ] 4. Infrastructure layer — EF Core DbContext, configurations, and repositories
  - [x] 4.1 Create UserMsDbContext with DbSets and SaveChangesAsync override for UpdatedAt
    - Configure default schema "user_ms"
    - Apply configurations from assembly
    - Override SaveChangesAsync to auto-set UpdatedAt on modified UserEntity
    - _Requirements: 14.5_

  - [x] 4.2 Create EF Core entity configurations (IEntityTypeConfiguration for each entity)
    - UserEntityConfiguration: table mapping, indexes (email unique, uuid unique, phone filtered unique), column constraints
    - LoginTokenEntityConfiguration: unique token index, composite index on (user_id, is_revoked)
    - ActionTokenEntityConfiguration: unique token index, enum conversion
    - AuthorizationCodeEntityConfiguration: unique code index
    - UserRoleEntityConfiguration: table mapping, foreign key
    - _Requirements: 14.1, 14.2, 14.3, 14.4_

  - [ ] 4.3 Create initial EF Core migration matching existing PostgreSQL schema
    - Generate migration that produces the exact same schema as the Java version
    - Use `migrationBuilder.Sql("-- Baseline")` approach for existing databases
    - _Requirements: 14.1, 14.2, 14.3_

  - [x] 4.4 Implement repository classes (UserRepository, LoginTokenRepository, ActionTokenRepository, AuthorizationCodeRepository)
    - Implement all interface methods using EF Core async operations
    - Use ConfigureAwait(false) in infrastructure layer
    - Implement PagedResult query with dynamic sorting and filtering
    - _Requirements: 9.3, 12.1, 12.2, 12.3, 14.1_

  - [ ]* 4.5 Write property tests for data uniqueness constraints
    - **Property 23: Data Uniqueness Constraints**
    - **Validates: Requirements 14.1, 14.2, 14.3, 14.4**

- [x] 5. Checkpoint - Verify infrastructure layer compiles and migrations generate
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 6. Configuration, validation, and error handling
  - [ ] 6.1 Create strongly-typed Options classes and appsettings.json
    - JwtOptions, OAuth2ClientOptions, SocialAuthOptions, RabbitMqOptions
    - Create appsettings.json and appsettings.Development.json with default values
    - Register Options in Program.cs via IServiceCollection.Configure<T>
    - _Requirements: 17.1, 17.2, 17.3_

  - [ ] 6.2 Implement FluentValidation validators
    - SignUpRequestValidator (email format, password complexity: min 8 chars, uppercase, lowercase, digit, special char)
    - UpdateProfileRequestValidator (field length limits)
    - CreateUserRequestValidator, UpdateUserRequestValidator, ChangePasswordRequestValidator
    - ResetPasswordRequestValidator, AdminChangePasswordRequest, AdminChangeEmailRequest validators
    - Register validators from assembly in DI
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

  - [ ] 6.3 Implement GlobalExceptionHandler (IExceptionHandler)
    - Map domain exceptions to HTTP status codes with ProblemDetails responses
    - Ensure unhandled exceptions return 500 without internal details
    - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 13.7, 13.8, 13.9_

  - [ ]* 6.4 Write property tests for password validation and exception mapping
    - **Property 4: Password Validation Rejects Weak Passwords**
    - **Property 5: Field Length Validation**
    - **Property 20: Exception-to-HTTP Status Code Mapping**
    - **Validates: Requirements 2.1, 2.2, 2.3, 2.4, 13.1–13.9**

- [ ] 7. Domain services — AuthService, UserService, TokenService
  - [ ] 7.1 Implement UserService
    - CreateUserAsync: hash password (BCrypt work factor 12), assign USER_MS_USER role, create action token, publish SendEmailCommand
    - GetUserByUuidAsync, UpdateProfileAsync (partial update), GetUsersPagedAsync
    - AdminCreateUserAsync, AdminUpdateUserAsync, DeleteUserAsync
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 8.1, 8.2, 8.3, 9.2, 9.3, 9.4, 9.5_

  - [ ] 7.2 Implement AuthService
    - ValidateCredentialsAsync: check existence, verify password, check IsActive, check IsEmailVerified, update LastLoginAt
    - VerifyEmailAsync, VerifyPhoneAsync: validate token, consume, set flag
    - ResendVerificationAsync: create new action token, publish SendEmailCommand
    - InitiatePasswordResetAsync, ResetPasswordAsync: token lifecycle, revoke all refresh tokens
    - ChangePasswordAsync: verify current password, update hash
    - AdminChangePasswordAsync, AdminChangeEmailAsync: admin operations without current password
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 6.3, 7.1, 7.2, 7.3, 7.4, 7.5, 9.6, 9.7, 10.1, 10.2, 10.3, 10.4, 10.5_

  - [ ] 7.3 Implement TokenService
    - CreateRefreshTokenAsync: generate 256-bit random token, store with 24h expiry
    - ValidateRefreshTokenAsync, RevokeRefreshTokenAsync, RevokeAllUserTokensAsync
    - CleanupExpiredTokensAsync: delete expired login tokens, action tokens, authorization codes
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 6.1, 12.1, 12.2, 12.3, 16.2_

  - [ ] 7.4 Implement SocialAuthService
    - HandleSocialLoginAsync: create new user or link provider to existing user by email
    - _Requirements: 11.1, 11.2, 11.3_

  - [ ]* 7.5 Write property tests for core service logic
    - **Property 1: Password Hashing Round-Trip**
    - **Property 2: Registration Postconditions**
    - **Property 3: Email Uniqueness Enforcement**
    - **Property 6: Action Token Single-Use**
    - **Property 7: Verification Sets Flag and Consumes Token**
    - **Property 12: Password Reset Revokes All Tokens**
    - **Property 13: Change Password Requires Correct Current Password**
    - **Property 14: Profile Partial Update**
    - **Property 18: Credential Validation State Checks**
    - **Property 19: Successful Authentication Updates LastLoginAt**
    - **Property 21: Token Entropy**
    - **Property 24: Timestamp Monotonicity**
    - **Property 25: Social Login Creates or Links User**
    - **Validates: Requirements 1.1–1.7, 3.1–3.4, 7.2–7.5, 8.1–8.3, 10.1–10.5, 11.1–11.2, 14.5, 16.1, 16.2**

- [ ] 8. Checkpoint - Verify domain services compile and unit tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 9. OpenIddict configuration and OAuth2 controllers
  - [ ] 9.1 Configure OpenIddict in Program.cs
    - Register OpenIddict Core (EF Core stores), Server (flows, endpoints, lifetimes, PKCE, signing keys), Validation
    - Set endpoint URIs: /oauth2/authorize, /oauth2/token, /oauth2/revoke, /oauth2/userinfo, discovery, JWKS
    - Configure token lifetimes (access 10min, refresh 24h, id 60min, auth code 10min)
    - Enforce PKCE (S256) and zero refresh token reuse leeway
    - _Requirements: 4.1, 4.5, 5.5, 15.1, 15.2, 16.6_

  - [ ] 9.2 Implement AuthorizationController (OpenIddict endpoints)
    - Authorize endpoint: validate client, redirect to login, issue authorization code
    - Token exchange endpoint: handle authorization_code, refresh_token, client_credentials grants
    - Revocation endpoint: mark tokens as revoked, always return 200
    - Userinfo endpoint: return authenticated user claims
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.6, 5.1, 5.2, 5.3, 5.4, 6.1, 6.2, 15.3_

  - [ ]* 9.3 Write property tests for OAuth2 flows
    - **Property 8: PKCE Verification**
    - **Property 9: Authorization Code Single-Use**
    - **Property 10: Refresh Token Strict Rotation**
    - **Property 11: Token Revocation Idempotence**
    - **Validates: Requirements 4.2, 4.4, 4.5, 4.6, 5.1, 5.4, 5.5, 6.1, 6.2**

- [ ] 10. API controllers, authentication, and middleware pipeline
  - [ ] 10.1 Implement AuthController (signup, verify-email, verify-phone, resend-verification, forgot-password, reset-password)
    - Wire to IAuthService and IUserService
    - Apply FluentValidation via filter or manual validation
    - Return appropriate status codes (201, 200, 400, 409, 410)
    - _Requirements: 1.7, 3.1, 3.2, 3.3, 3.4, 3.5, 7.1, 7.2_

  - [ ] 10.2 Implement ProfileController (update-profile, change-password)
    - Require [Authorize] attribute
    - Use ICurrentUserService to resolve caller UUID
    - _Requirements: 7.4, 7.5, 8.1, 8.2, 8.3_

  - [ ] 10.3 Implement UsersController (admin CRUD, change-password, change-email)
    - Require [Authorize(Roles = "USER_MS_ADMIN,SUPER_ADMIN")]
    - Implement paginated list, create, get, update, delete, admin-change-password, admin-change-email
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7_

  - [ ] 10.4 Configure Program.cs middleware pipeline and DI registration
    - Register all services, repositories, validators, MassTransit, health checks
    - Configure middleware order: ExceptionHandler → HTTPS → CORS → Authentication → Authorization → RateLimiter → Controllers
    - Configure rate limiting policies (login: 5/min/IP, registration: 3/hr/IP, password reset: 3/hr/email)
    - Add social auth providers (Google, GitHub, LinkedIn)
    - Register TokenCleanupService as hosted service
    - Map health check endpoints (/health, /alive)
    - _Requirements: 16.3, 16.4, 16.5, 18.1, 18.2_

  - [ ]* 10.5 Write property test for admin authorization gate
    - **Property 15: Admin Authorization Gate**
    - **Property 16: Cascade Deletion**
    - **Property 17: Admin Email Change Resets Verification**
    - **Validates: Requirements 9.1, 9.5, 9.7**

- [ ] 11. Background services and token cleanup
  - [ ] 11.1 Implement TokenCleanupService (BackgroundService)
    - Run hourly to delete expired login tokens, action tokens, and authorization codes
    - Use IServiceScopeFactory for scoped repository access
    - Respect CancellationToken for graceful shutdown
    - _Requirements: 12.1, 12.2, 12.3_

  - [ ]* 11.2 Write property test for token cleanup
    - **Property 22: Token Cleanup Removes Only Expired Tokens**
    - **Validates: Requirements 12.1, 12.2, 12.3**

- [ ] 12. Checkpoint - Verify full application compiles and starts
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 13. Integration tests
  - [ ]* 13.1 Write integration tests for auth endpoints (signup, verify, password reset flows)
    - Use WebApplicationFactory<Program> with Testcontainers PostgreSQL
    - Test full HTTP request/response cycle
    - Verify status codes, response bodies, and database state
    - _Requirements: 1.1, 1.6, 1.7, 3.1, 3.3, 7.1, 7.2_

  - [ ]* 13.2 Write integration tests for admin user management endpoints
    - Test CRUD operations with proper role-based authorization
    - Test pagination, filtering, cascade deletion
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_

  - [ ]* 13.3 Write integration tests for OAuth2/OIDC flows
    - Test authorization code + PKCE flow end-to-end
    - Test refresh token rotation
    - Test token revocation
    - Test discovery and JWKS endpoints
    - _Requirements: 4.1, 4.2, 4.3, 5.1, 5.2, 6.1, 15.1, 15.2_

- [ ] 14. Final checkpoint - Ensure all tests pass and application starts correctly
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties from the design document
- Unit tests validate specific examples and edge cases
- The implementation uses C# 13 / .NET 9 with async/await throughout
- All I/O methods follow the `Async` suffix convention with CancellationToken parameters
- EF Core migrations target the existing `user_ms` PostgreSQL schema for backward compatibility

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2", "1.3"] },
    { "id": 2, "tasks": ["1.4"] },
    { "id": 3, "tasks": ["1.5"] },
    { "id": 4, "tasks": ["3.1", "3.2"] },
    { "id": 5, "tasks": ["3.3", "3.4"] },
    { "id": 6, "tasks": ["4.1", "4.2"] },
    { "id": 7, "tasks": ["4.3", "4.4"] },
    { "id": 8, "tasks": ["4.5", "6.1"] },
    { "id": 9, "tasks": ["6.2", "6.3"] },
    { "id": 10, "tasks": ["6.4", "7.1", "7.3"] },
    { "id": 11, "tasks": ["7.2", "7.4"] },
    { "id": 12, "tasks": ["7.5", "9.1"] },
    { "id": 13, "tasks": ["9.2", "11.1"] },
    { "id": 14, "tasks": ["9.3", "10.1", "10.2", "10.3"] },
    { "id": 15, "tasks": ["10.4"] },
    { "id": 16, "tasks": ["10.5", "11.2"] },
    { "id": 17, "tasks": ["13.1", "13.2", "13.3"] }
  ]
}
```
