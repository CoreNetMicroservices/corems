using CoreMs.DocumentMs.Core.Entities;
using CoreMs.DocumentMs.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreMs.DocumentMs.Infrastructure.Data.Configurations;

public class DocumentEntityConfiguration : IEntityTypeConfiguration<DocumentEntity>
{
    public void Configure(EntityTypeBuilder<DocumentEntity> builder)
    {
        builder.ToTable("documents");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).UseIdentityAlwaysColumn();

        builder.HasIndex(e => e.Uuid).IsUnique();
        builder.HasIndex(e => e.ObjectKey).IsUnique();
        builder.HasIndex(e => new { e.UserId, e.OriginalFilename });

        builder.Property(e => e.Uuid).IsRequired();
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(255);
        builder.Property(e => e.OriginalFilename).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Size).IsRequired();
        builder.Property(e => e.Extension).IsRequired().HasMaxLength(20);
        builder.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Bucket).IsRequired().HasMaxLength(100);
        builder.Property(e => e.ObjectKey).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Visibility).IsRequired().HasDefaultValue(DocumentVisibility.Private);
        builder.Property(e => e.UploadedByType).IsRequired().HasDefaultValue(UploadedByType.User);
        builder.Property(e => e.Checksum).HasMaxLength(128);
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Version).IsRequired().HasDefaultValue(1);
        builder.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(e => e.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("NOW()");

        // Tags stored as PostgreSQL jsonb
        builder.Property(e => e.Tags)
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValueSql("'[]'::jsonb");
    }
}
