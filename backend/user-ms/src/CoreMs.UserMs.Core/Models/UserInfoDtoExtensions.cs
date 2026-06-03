using CoreMs.UserMs.Core.Entities;

namespace CoreMs.UserMs.Core.Models;

public static class UserInfoDtoExtensions
{
    public static UserInfoDto ToUserInfoDto(this UserEntity entity) => new()
    {
        Uuid = entity.Uuid,
        Email = entity.Email,
        FirstName = entity.FirstName,
        LastName = entity.LastName,
        PhoneNumber = entity.PhoneNumber,
        ImageUrl = entity.ImageUrl,
        Provider = entity.Provider,
        EmailVerified = entity.EmailVerified,
        PhoneVerified = entity.PhoneVerified,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        LastLoginAt = entity.LastLoginAt,
        Roles = entity.Roles.Select(r => r.Name).ToList()
    };
}
