using System.Text.Json;
using CoreMs.TranslationMs.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CoreMs.TranslationMs.Infrastructure.Data.Configurations;

public class TranslationBundleEntityConfiguration : IEntityTypeConfiguration<TranslationBundleEntity>
{
    public void Configure(EntityTypeBuilder<TranslationBundleEntity> builder)
    {
        builder.ToTable("translation_bundles");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).UseIdentityAlwaysColumn();

        builder.HasIndex(e => new { e.Realm, e.Lang }).IsUnique();
        builder.HasIndex(e => e.Realm);
        builder.HasIndex(e => e.Lang);

        builder.Property(e => e.Realm).IsRequired().HasMaxLength(255);
        builder.Property(e => e.Lang).IsRequired().HasMaxLength(10);
        builder.Property(e => e.Data)
            .HasColumnType("jsonb")
            .IsRequired()
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new());
        builder.Property(e => e.UpdatedAt).IsRequired().HasDefaultValueSql("NOW()");
    }
}
