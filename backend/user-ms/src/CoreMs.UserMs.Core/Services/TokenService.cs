using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CoreMs.Common.Exceptions;
using CoreMs.Common.Extensions;
using CoreMs.UserMs.Core.Configuration;
using CoreMs.UserMs.Core.Entities;
using CoreMs.UserMs.Core.Exceptions;
using CoreMs.UserMs.Core.Models;
using CoreMs.UserMs.Core.Repositories;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CoreMs.UserMs.Core.Services;

[Service]
public class TokenService
{
    private readonly LoginTokenRepository _loginTokenRepository;
    private readonly ActionTokenRepository _actionTokenRepository;
    private readonly AuthorizationCodeRepository _authorizationCodeRepository;
    private readonly TokenServiceOptions _options;
    private readonly SigningCredentials _signingCredentials;

    public TokenService(
        LoginTokenRepository loginTokenRepository,
        ActionTokenRepository actionTokenRepository,
        AuthorizationCodeRepository authorizationCodeRepository,
        IOptions<TokenServiceOptions> options)
    {
        _loginTokenRepository = loginTokenRepository;
        _actionTokenRepository = actionTokenRepository;
        _authorizationCodeRepository = authorizationCodeRepository;
        _options = options.Value;
        _signingCredentials = CreateSigningCredentials();
    }

    public Task<OAuth2TokenResponse> GenerateTokenResponseAsync(UserEntity user, string? scope, string? nonce, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var scopes = scope ?? "openid profile email";

        var accessToken = GenerateAccessToken(user, scopes, now);
        var refreshToken = CreateAndPersistRefreshToken(user);

        string? idToken = null;
        if (scopes.Contains("openid"))
            idToken = GenerateIdToken(user, nonce, now);

        var response = new OAuth2TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            IdToken = idToken,
            TokenType = "Bearer",
            ExpiresIn = _options.AccessTokenExpirationMinutes * 60,
            Scope = scopes
        };

        return Task.FromResult(response);
    }

    public Task<string> CreateRefreshTokenAsync(UserEntity user, CancellationToken ct = default)
    {
        var token = CreateAndPersistRefreshToken(user);
        return Task.FromResult(token);
    }

    public async Task ValidateRefreshTokenAsync(Guid tokenId, Guid userUuid, CancellationToken ct = default)
    {
        var loginToken = await _loginTokenRepository.GetByUuidAsync(tokenId, ct)
            ?? throw ServiceException.Of(UserErrors.TokenNotFound, $"Token not found with ID: {tokenId}");

        if (loginToken.User.Uuid != userUuid)
            throw ServiceException.Of(UserErrors.TokenNotFound, $"Token not found with ID: {tokenId}");
    }

    public async Task RevokeAllUserTokensAsync(long userId, CancellationToken ct = default)
    {
        await _loginTokenRepository.DeleteAllByUserIdAsync(userId, ct);
    }

    public async Task CleanupExpiredTokensAsync(CancellationToken ct = default)
    {
        await _loginTokenRepository.DeleteExpiredAsync(ct);
        await _actionTokenRepository.DeleteExpiredAsync(ct);
        await _authorizationCodeRepository.DeleteExpiredAsync(ct);
    }

    private string GenerateAccessToken(UserEntity user, string scopes, DateTime now)
    {
        var roles = user.Roles.Select(r => r.Name).ToList();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Uuid.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("scope", scopes)
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = now.AddMinutes(_options.AccessTokenExpirationMinutes),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = _signingCredentials,
            IssuedAt = now,
            NotBefore = now
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GenerateIdToken(UserEntity user, string? nonce, DateTime now)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Uuid.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("email_verified", user.EmailVerified.ToString().ToLowerInvariant(), ClaimValueTypes.Boolean)
        };

        if (user.FirstName is not null)
            claims.Add(new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName));

        if (user.LastName is not null)
            claims.Add(new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName));

        if (nonce is not null)
            claims.Add(new Claim(JwtRegisteredClaimNames.Nonce, nonce));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = now.AddMinutes(_options.IdTokenExpirationMinutes),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            SigningCredentials = _signingCredentials,
            IssuedAt = now,
            NotBefore = now
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string CreateAndPersistRefreshToken(UserEntity user)
    {
        var token = GenerateSecureToken();

        var loginToken = new LoginTokenEntity
        {
            UserId = user.Id,
            Token = token,
            User = user
        };
        _loginTokenRepository.Add(loginToken);

        return token;
    }

    private SigningCredentials CreateSigningCredentials()
    {
        if (string.IsNullOrEmpty(_options.SecretKey))
        {
            var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32));
            return new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            {
                CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
            };
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        return new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
