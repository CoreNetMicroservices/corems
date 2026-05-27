using CoreMs.UserMs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreMs.UserMs.Infrastructure.Data.Configurations;

public class AuthorizationCodeEntityConfiguration : IEntityTypeConfiguration<AuthorizationCodeEntity>
{
    public void Configure(EntityTypeBuilder<AuthorizationCodeEntity> builder)
    {
        builder.ToTable("authorization_codes", "user_ms");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();

        builder.HasIndex(e => e.Code).IsUnique().HasDatabaseName("idx_authorization_codes_code");
        builder.HasIndex(e => e.UserId).HasDatabaseName("idx_authorization_codes_user_id");
        builder.HasIndex(e => e.ExpiresAt).HasDatabaseName("idx_authorization_codes_expires_at");

        builder.Property(e => e.Code).HasColumnName("code").IsRequired().HasMaxLength(255);
        builder.Property(e => e.ClientId).HasColumnName("client_id").IsRequired().HasMaxLength(255);
        builder.Property(e => e.RedirectUri).HasColumnName("redirect_uri").IsRequired().HasMaxLength(500);
        builder.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(e => e.Scope).HasColumnName("scope").HasMaxLength(500);
        builder.Property(e => e.CodeChallenge).HasColumnName("code_challenge").HasMaxLength(255);
        builder.Property(e => e.CodeChallengeMethod).HasColumnName("code_challenge_method").HasMaxLength(10);
        builder.Property(e => e.Nonce).HasColumnName("nonce").HasMaxLength(255);
        builder.Property(e => e.State).HasColumnName("state").HasMaxLength(255);
        builder.Property(e => e.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(e => e.IsUsed).HasColumnName("is_used").IsRequired().HasDefaultValue(false);
        builder.Property(e => e.UsedAt).HasColumnName("used_at");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
