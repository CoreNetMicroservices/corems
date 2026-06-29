using CoreMs.DocumentMs.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreMs.DocumentMs.Infrastructure.Data.Configurations;

public class DocumentAccessTokenEntityConfiguration : IEntityTypeConfiguration<DocumentAccessTokenEntity>
{
    public void Configure(EntityTypeBuilder<DocumentAccessTokenEntity> builder)
    {
        builder.ToTable("document_access_tokens");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).UseIdentityAlwaysColumn();

        builder.HasIndex(e => new { e.TokenHash, e.DocumentUuid });

        builder.Property(e => e.DocumentUuid).IsRequired();
        builder.Property(e => e.TokenHash).IsRequired().HasMaxLength(128);
        builder.Property(e => e.CreatedBy).IsRequired();
        builder.Property(e => e.ExpiresAt).IsRequired();
        builder.Property(e => e.IsRevoked).IsRequired().HasDefaultValue(false);
        builder.Property(e => e.AccessCount).IsRequired().HasDefaultValue(0);
        builder.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
    }
}
