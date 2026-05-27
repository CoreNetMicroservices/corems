using CoreMs.UserMs.Domain.Entities;
using CoreMs.UserMs.Domain.Models;

namespace CoreMs.UserMs.Domain.Interfaces;

/// <summary>
/// Handles JWT/refresh token generation, validation, revocation, and cleanup.
/// </summary>
public interface ITokenService
{
    Task<OAuth2TokenResponse> GenerateTokenResponseAsync(UserEntity user, string? scope, string? nonce, CancellationToken ct = default);
    Task<string> CreateRefreshTokenAsync(UserEntity user, CancellationToken ct = default);
    Task ValidateRefreshTokenAsync(Guid tokenId, Guid userUuid, CancellationToken ct = default);
    Task RevokeAllUserTokensAsync(long userId, CancellationToken ct = default);
    Task CleanupExpiredTokensAsync(CancellationToken ct = default);
}
