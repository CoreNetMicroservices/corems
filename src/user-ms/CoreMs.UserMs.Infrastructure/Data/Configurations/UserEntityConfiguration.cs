using CoreMs.UserMs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreMs.UserMs.Infrastructure.Data.Configurations;

public class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("app_user", "user_ms");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();

        builder.HasIndex(e => e.Uuid).IsUnique();
        builder.HasIndex(e => e.Email).IsUnique();
        builder.HasIndex(e => e.PhoneNumber).IsUnique();
        builder.HasIndex(e => e.CreatedAt);

        builder.Property(e => e.Uuid).HasColumnName("uuid").IsRequired();
        builder.Property(e => e.Provider).HasColumnName("provider").IsRequired().HasMaxLength(255);
        builder.Property(e => e.Email).HasColumnName("email").IsRequired().HasMaxLength(255);
        builder.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(50);
        builder.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(50);
        builder.Property(e => e.ImageUrl).HasColumnName("image_url").HasMaxLength(255);
        builder.Property(e => e.PhoneNumber).HasColumnName("phone_number").HasMaxLength(50);
        builder.Property(e => e.Password).HasColumnName("password").HasMaxLength(255);
        builder.Property(e => e.EmailVerified).HasColumnName("email_verified").IsRequired().HasDefaultValue(false);
        builder.Property(e => e.PhoneVerified).HasColumnName("phone_verified");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.LastLoginAt).HasColumnName("last_login_at");

        builder.HasMany(e => e.Roles)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Tokens)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.ActionTokens)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
