using CoreMs.Common.Exceptions;
using CoreMs.Common.Extensions;
using CoreMs.UserMs.Core.Entities;
using CoreMs.UserMs.Core.Exceptions;
using CoreMs.UserMs.Core.Models;
using CoreMs.UserMs.Core.Repositories;

namespace CoreMs.UserMs.Core.Services;

[Service]
public class SocialAuthService(UserRepository userRepository)
{
    private static readonly HashSet<string> SupportedProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "google", "github", "linkedin"
    };

    public async Task<UserEntity> HandleSocialLoginAsync(
        string provider,
        ExternalLoginInfo info,
        CancellationToken ct = default)
    {
        if (!SupportedProviders.Contains(provider))
            throw ServiceException.Of(UserErrors.InvalidRequest, $"Unsupported social provider: {provider}");

        if (string.IsNullOrWhiteSpace(info.Email))
            throw ServiceException.Of(UserErrors.InvalidRequest, "Email not provided by social provider");

        var normalizedProvider = provider.ToLowerInvariant();
        var existingUser = await userRepository.GetByEmailAsync(info.Email, ct);

        if (existingUser is not null)
            return LinkProviderToExistingUser(existingUser, normalizedProvider, info);

        return CreateUserFromSocialLogin(normalizedProvider, info);
    }

    private static UserEntity LinkProviderToExistingUser(
        UserEntity user,
        string provider,
        ExternalLoginInfo info)
    {
        if (!user.Provider.Contains(provider, StringComparison.OrdinalIgnoreCase))
        {
            user.Provider = string.IsNullOrEmpty(user.Provider)
                ? provider
                : $"{user.Provider},{provider}";
        }

        if (info.ImageUrl is not null && user.ImageUrl is null)
            user.ImageUrl = info.ImageUrl;

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        return user;
    }

    private UserEntity CreateUserFromSocialLogin(string provider, ExternalLoginInfo info)
    {
        var user = new UserEntity
        {
            Email = info.Email,
            FirstName = info.FirstName,
            LastName = info.LastName,
            ImageUrl = info.ImageUrl,
            Provider = provider,
            EmailVerified = true,
            LastLoginAt = DateTime.UtcNow
        };

        user.Roles.Add(new UserRoleEntity { Name = "USER_MS_USER" });
        userRepository.Add(user);

        return user;
    }
}
