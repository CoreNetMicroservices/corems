using CoreMs.UserMs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreMs.UserMs.Infrastructure.Data.Configurations;

public class LoginTokenEntityConfiguration : IEntityTypeConfiguration<LoginTokenEntity>
{
    public void Configure(EntityTypeBuilder<LoginTokenEntity> builder)
    {
        builder.ToTable("login_token", "user_ms");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();

        builder.HasIndex(e => e.Uuid).IsUnique();
        builder.HasIndex(e => e.Token).IsUnique();
        builder.HasIndex(e => e.UserId).HasDatabaseName("idx_login_token_user");
        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("idx_login_token_created_at");

        builder.Property(e => e.Uuid).HasColumnName("uuid").IsRequired();
        builder.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(e => e.Token).HasColumnName("token").IsRequired().HasColumnType("text");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
