# Requirements Document

## Introduction

This document defines the functional requirements for the User Management Service (user-ms) migrated from Java Spring Boot to C# / ASP.NET Core 9. The service is an OAuth2/OIDC Authorization Server responsible for user authentication, authorization, token management, profile operations, and administrative user CRUD. It uses OpenIddict as the embedded OAuth2/OIDC provider, Entity Framework Core for data access, and MassTransit for asynchronous messaging.

## Glossary

- **User_Service**: The `UserService` class within user-ms — a domain service handling user registration, profile updates, and admin CRUD operations (injected as `IUserService` via DI)
- **Auth_Service**: The `AuthService` class within user-ms — a domain service handling credential validation, email/phone verification, and password operations (injected as `IAuthService` via DI)
- **Token_Service**: The `TokenService` class within user-ms — a domain service managing refresh token lifecycle: creation, validation, revocation, and cleanup (injected as `ITokenService` via DI)
- **User_Repository**: The data access layer for user entity persistence (EF Core repository implementing `IUserRepository`)
- **OpenIddict_Server**: The OpenIddict middleware configured within user-ms — provides embedded OAuth2/OIDC authorization server capabilities (not a separate service)
- **Validation_Pipeline**: FluentValidation middleware in the ASP.NET Core request pipeline — validates all incoming request DTOs before they reach domain services
- **Exception_Handler**: The `GlobalExceptionHandler` middleware registered in user-ms — maps domain exceptions to standardized HTTP ProblemDetails responses
- **Token_Cleanup_Service**: A `BackgroundService` hosted within user-ms — runs periodically to delete expired tokens from the database
- **Message_Bus**: MassTransit `IPublishEndpoint` injected via DI — used for fire-and-forget messaging to Communication MS over RabbitMQ

## Requirements

### Requirement 1: User Registration

**User Story:** As a new user, I want to register with my email and password, so that I can create an account and access the system.

#### Acceptance Criteria

1. WHEN a valid sign-up request is received, THE User_Service SHALL create a new user with IsActive set to true and IsEmailVerified set to false
2. WHEN a valid sign-up request is received, THE User_Service SHALL hash the password using BCrypt before persisting
3. WHEN a valid sign-up request is received, THE User_Service SHALL assign the default role USER_MS_USER to the new user
4. WHEN a valid sign-up request is received, THE User_Service SHALL create an action token of type EmailVerification with a 24-hour expiry
5. WHEN a valid sign-up request is received, THE User_Service SHALL publish a SendEmailCommand to the Message_Bus with the verification token
6. WHEN a sign-up request contains an email that already exists, THE User_Service SHALL return HTTP 409 Conflict
7. WHEN a valid sign-up request is processed successfully, THE User_Service SHALL return HTTP 201 Created with the user UUID and email

### Requirement 2: Input Validation

**User Story:** As a system operator, I want all incoming requests validated before processing, so that invalid data never reaches the domain layer.

#### Acceptance Criteria

1. WHEN a sign-up request has an empty or invalid email format, THE Validation_Pipeline SHALL reject the request with HTTP 400 and a descriptive error
2. WHEN a sign-up request has a password shorter than 8 characters, THE Validation_Pipeline SHALL reject the request with HTTP 400
3. WHEN a sign-up request has a password missing an uppercase letter, lowercase letter, digit, or special character, THE Validation_Pipeline SHALL reject the request with HTTP 400
4. WHEN a profile update request has a field exceeding its maximum length, THE Validation_Pipeline SHALL reject the request with HTTP 400
5. THE Validation_Pipeline SHALL validate all request DTOs before they reach the domain service layer

### Requirement 3: Email and Phone Verification

**User Story:** As a registered user, I want to verify my email and phone, so that I can prove ownership and unlock full account capabilities.

#### Acceptance Criteria

1. WHEN a valid email verification token is submitted, THE Auth_Service SHALL mark the user's IsEmailVerified as true and consume the action token
2. WHEN a valid phone verification token is submitted, THE Auth_Service SHALL mark the user's IsPhoneVerified as true and consume the action token
3. WHEN an expired action token is submitted, THE Auth_Service SHALL return HTTP 410 Gone with a token expired message
4. WHEN an already-consumed action token is submitted, THE Auth_Service SHALL return HTTP 410 Gone with a token already used message
5. WHEN a resend verification request is received for a valid email, THE Auth_Service SHALL create a new action token and publish a SendEmailCommand to the Message_Bus

### Requirement 4: OAuth2 Authorization Code Flow with PKCE

**User Story:** As a client application, I want to authenticate users via Authorization Code + PKCE, so that I can securely obtain tokens without exposing credentials.

#### Acceptance Criteria

1. WHEN an authorization request is received, THE OpenIddict_Server SHALL validate the client_id, redirect_uri, and code_challenge parameters
2. WHEN a valid authorization code exchange is requested, THE OpenIddict_Server SHALL verify the code_verifier against the stored code_challenge using S256
3. WHEN PKCE verification succeeds, THE OpenIddict_Server SHALL issue an access token (10 min), refresh token (24 h), and id token (60 min)
4. WHEN an authorization code is exchanged, THE OpenIddict_Server SHALL mark the code as consumed so it cannot be reused
5. THE OpenIddict_Server SHALL require PKCE (S256 method) for all public client authorization requests
6. WHEN an authorization code has expired (older than 10 minutes), THE OpenIddict_Server SHALL reject the token exchange

### Requirement 5: Refresh Token Rotation

**User Story:** As a client application, I want to refresh my access token using a refresh token, so that users remain authenticated without re-entering credentials.

#### Acceptance Criteria

1. WHEN a valid refresh token is presented, THE OpenIddict_Server SHALL revoke the old refresh token and issue a new refresh token (strict rotation)
2. WHEN a valid refresh token is presented, THE OpenIddict_Server SHALL issue a new access token and id token alongside the new refresh token
3. WHEN an expired refresh token is presented, THE OpenIddict_Server SHALL reject the request
4. WHEN a revoked refresh token is presented, THE OpenIddict_Server SHALL reject the request
5. THE OpenIddict_Server SHALL enforce zero reuse leeway on refresh tokens

### Requirement 6: Token Revocation

**User Story:** As a client application, I want to revoke tokens, so that I can invalidate sessions on logout or security events.

#### Acceptance Criteria

1. WHEN a token revocation request is received with a valid token, THE OpenIddict_Server SHALL mark the token as revoked in the database
2. WHEN a token revocation request is received, THE OpenIddict_Server SHALL return HTTP 200 regardless of whether the token was found
3. WHEN a user's password is reset, THE Auth_Service SHALL revoke all existing refresh tokens for that user

### Requirement 7: Password Management

**User Story:** As a user, I want to change or reset my password, so that I can maintain account security.

#### Acceptance Criteria

1. WHEN a forgot-password request is received for a valid email, THE Auth_Service SHALL create a PasswordReset action token and publish a SendEmailCommand to the Message_Bus
2. WHEN a valid password reset token and new password are submitted, THE Auth_Service SHALL update the user's password hash and consume the action token
3. WHEN a password reset is completed, THE Auth_Service SHALL revoke all existing refresh tokens for the user
4. WHEN an authenticated user submits a change-password request with a correct current password, THE Auth_Service SHALL update the password hash
5. WHEN an authenticated user submits a change-password request with an incorrect current password, THE Auth_Service SHALL reject the request

### Requirement 8: User Profile Management

**User Story:** As an authenticated user, I want to update my profile information, so that I can keep my account details current.

#### Acceptance Criteria

1. WHEN an authenticated user submits a profile update, THE User_Service SHALL update only the provided fields (firstName, lastName, phone, avatarUrl)
2. WHEN a profile is updated, THE User_Service SHALL set the UpdatedAt timestamp to the current UTC time
3. WHEN a profile update is successful, THE User_Service SHALL return the updated user information as a UserInfoDto

### Requirement 9: Administrative User Management

**User Story:** As an administrator, I want to create, read, update, and delete users, so that I can manage the user base.

#### Acceptance Criteria

1. WHILE a request is made to the admin users endpoint, THE User_Service SHALL require the caller to have the USER_MS_ADMIN or SUPER_ADMIN role
2. WHEN an admin creates a user, THE User_Service SHALL persist the user with the specified roles and return the user information
3. WHEN an admin requests a paginated user list, THE User_Service SHALL return a PagedResult containing matching users
4. WHEN an admin updates a user, THE User_Service SHALL update the specified fields including IsActive status and roles
5. WHEN an admin deletes a user, THE User_Service SHALL remove the user and cascade-delete all related tokens, roles, and action tokens
6. WHEN an admin changes a user's password, THE Auth_Service SHALL update the password hash without requiring the current password
7. WHEN an admin changes a user's email, THE Auth_Service SHALL update the email and reset the IsEmailVerified flag to false

### Requirement 10: Credential Validation

**User Story:** As the system, I want to validate user credentials securely, so that only legitimate users can authenticate.

#### Acceptance Criteria

1. WHEN valid credentials are submitted, THE Auth_Service SHALL return the user entity and update LastLoginAt
2. WHEN credentials are submitted for a non-existent email, THE Auth_Service SHALL throw an AuthenticationException
3. WHEN credentials are submitted with an incorrect password, THE Auth_Service SHALL throw an AuthenticationException
4. WHEN credentials are submitted for a disabled account (IsActive = false), THE Auth_Service SHALL throw an AccountDisabledException
5. WHEN credentials are submitted for an unverified email (IsEmailVerified = false), THE Auth_Service SHALL throw an EmailNotVerifiedException

### Requirement 11: Social OAuth2 Login

**User Story:** As a user, I want to sign in with Google, GitHub, or LinkedIn, so that I can use my existing social accounts.

#### Acceptance Criteria

1. WHEN a social login is completed for a new user, THE User_Service SHALL create a user entity with the social provider and provider ID populated
2. WHEN a social login is completed for an existing user (matched by email), THE User_Service SHALL link the social provider to the existing account
3. THE User_Service SHALL support Google, GitHub, and LinkedIn as social authentication providers

### Requirement 12: Token Lifecycle Management

**User Story:** As a system operator, I want expired tokens cleaned up automatically, so that the database does not grow unbounded.

#### Acceptance Criteria

1. THE Token_Cleanup_Service SHALL run periodically (every hour) to delete expired login tokens
2. THE Token_Cleanup_Service SHALL delete expired action tokens during each cleanup cycle
3. THE Token_Cleanup_Service SHALL delete expired authorization codes during each cleanup cycle

### Requirement 13: Error Handling

**User Story:** As a client developer, I want consistent error responses, so that I can handle failures predictably.

#### Acceptance Criteria

1. WHEN an AuthenticationException occurs, THE Exception_Handler SHALL return HTTP 401 with a ProblemDetails response
2. WHEN an AccountDisabledException occurs, THE Exception_Handler SHALL return HTTP 403
3. WHEN an EmailNotVerifiedException occurs, THE Exception_Handler SHALL return HTTP 403
4. WHEN an EntityNotFoundException occurs, THE Exception_Handler SHALL return HTTP 404
5. WHEN a DuplicateEmailException occurs, THE Exception_Handler SHALL return HTTP 409
6. WHEN a TokenExpiredException occurs, THE Exception_Handler SHALL return HTTP 410
7. WHEN a TokenConsumedException occurs, THE Exception_Handler SHALL return HTTP 410
8. WHEN a ValidationException occurs, THE Exception_Handler SHALL return HTTP 400 with formatted validation errors
9. WHEN an unhandled exception occurs, THE Exception_Handler SHALL return HTTP 500 without exposing internal details

### Requirement 14: Data Integrity Constraints

**User Story:** As a system operator, I want data integrity enforced at the database level, so that invalid states cannot exist regardless of application bugs.

#### Acceptance Criteria

1. THE User_Repository SHALL enforce email uniqueness via a unique database index
2. THE User_Repository SHALL enforce UUID uniqueness via a unique database index
3. THE User_Repository SHALL enforce phone uniqueness via a filtered unique index (where phone is not null)
4. THE Token_Service SHALL enforce token value uniqueness via a unique database index
5. WHEN a user entity is modified, THE User_Service SHALL automatically update the UpdatedAt timestamp

### Requirement 15: OpenID Connect Discovery

**User Story:** As a client application, I want to discover the authorization server's capabilities, so that I can configure OAuth2 flows dynamically.

#### Acceptance Criteria

1. THE OpenIddict_Server SHALL serve an OpenID Connect discovery document at /.well-known/openid-configuration
2. THE OpenIddict_Server SHALL serve the JSON Web Key Set at /.well-known/jwks.json
3. THE OpenIddict_Server SHALL serve user claims at the /oauth2/userinfo endpoint for authenticated requests

### Requirement 16: Security Controls

**User Story:** As a security engineer, I want defense-in-depth controls, so that the system resists common attack vectors.

#### Acceptance Criteria

1. THE User_Service SHALL hash all passwords using BCrypt with a work factor of 12
2. THE Token_Service SHALL generate refresh tokens with at least 256 bits of cryptographic randomness
3. THE User_Service SHALL enforce rate limiting on login attempts (5 per minute per IP)
4. THE User_Service SHALL enforce rate limiting on registration (3 per hour per IP)
5. THE User_Service SHALL enforce rate limiting on password reset requests (3 per hour per email)
6. THE OpenIddict_Server SHALL sign tokens using RS256 in production

### Requirement 17: Configuration Management

**User Story:** As a developer, I want strongly-typed configuration with environment overrides, so that the service is configurable across environments without code changes.

#### Acceptance Criteria

1. THE User_Service SHALL load configuration from appsettings.json with environment variable overrides
2. THE User_Service SHALL bind configuration sections to strongly-typed Options classes (JwtOptions, OAuth2ClientOptions, SocialAuthOptions, RabbitMqOptions)
3. THE User_Service SHALL support .NET User Secrets for development-time secret storage

### Requirement 18: Health Monitoring

**User Story:** As a platform operator, I want health check endpoints, so that I can monitor service availability and readiness.

#### Acceptance Criteria

1. THE User_Service SHALL expose a /health endpoint that checks PostgreSQL and RabbitMQ connectivity
2. THE User_Service SHALL expose an /alive endpoint for liveness probes
