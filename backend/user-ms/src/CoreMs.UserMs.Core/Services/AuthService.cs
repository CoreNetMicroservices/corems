using CoreMs.Common.Exceptions;
using CoreMs.Common.Extensions;
using CoreMs.UserMs.Core.Entities;
using CoreMs.UserMs.Core.Exceptions;
using CoreMs.UserMs.Core.Repositories;

namespace CoreMs.UserMs.Core.Services;

[Service]
public class AuthService(UserRepository userRepository)
{
    public async Task<UserEntity> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await userRepository.GetByEmailAsync(email, ct)
            ?? throw ServiceException.Of(UserErrors.InvalidCredentials, "Invalid credentials");

        if (user.Password is null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
            throw ServiceException.Of(UserErrors.InvalidCredentials, "Invalid credentials");

        if (!user.EmailVerified)
            throw ServiceException.Of(UserErrors.EmailNotVerified, "Email not verified");

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(user);

        return user;
    }
}
