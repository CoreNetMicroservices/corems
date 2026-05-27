using CoreMs.UserMs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreMs.UserMs.Infrastructure.Data.Configurations;

public class ActionTokenEntityConfiguration : IEntityTypeConfiguration<ActionTokenEntity>
{
    public void Configure(EntityTypeBuilder<ActionTokenEntity> builder)
    {
        builder.ToTable("action_tokens", "user_ms");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();

        builder.HasIndex(e => e.Uuid).IsUnique();
        builder.HasIndex(e => e.TokenHash).IsUnique().HasDatabaseName("idx_action_tokens_token_hash");
        builder.HasIndex(e => e.UserId).HasDatabaseName("idx_action_tokens_user_id");

        builder.Property(e => e.Uuid).HasColumnName("uuid").IsRequired();
        builder.Property(e => e.TokenHash).HasColumnName("token_hash").IsRequired().HasMaxLength(64);
        builder.Property(e => e.ActionType).HasColumnName("action_type").IsRequired().HasMaxLength(50)
            .HasConversion<string>();
        builder.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(e => e.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(e => e.Used).HasColumnName("used").IsRequired().HasDefaultValue(false);
        builder.Property(e => e.UsedAt).HasColumnName("used_at");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
