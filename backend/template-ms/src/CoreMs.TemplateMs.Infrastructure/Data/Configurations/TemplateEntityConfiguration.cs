using System.Text.Json;
using CoreMs.TemplateMs.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreMs.TemplateMs.Infrastructure.Data.Configurations;

public class TemplateEntityConfiguration : IEntityTypeConfiguration<TemplateEntity>
{
    public void Configure(EntityTypeBuilder<TemplateEntity> builder)
    {
        builder.ToTable("templates", "template_ms");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();

        builder.HasIndex(e => e.Uuid).IsUnique();
        builder.HasIndex(e => new { e.TemplateId, e.Language }).IsUnique();
        builder.HasIndex(e => e.CreatedAt);

        builder.Property(e => e.Uuid).HasColumnName("uuid").IsRequired();
        builder.Property(e => e.TemplateId).HasColumnName("template_id").IsRequired().HasMaxLength(255);
        builder.Property(e => e.Language).HasColumnName("language").IsRequired().HasMaxLength(10).HasDefaultValue("en");
        builder.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(255);
        builder.Property(e => e.Description).HasColumnName("description");
        builder.Property(e => e.Content).HasColumnName("content").IsRequired();
        builder.Property(e => e.Category).HasColumnName("category").IsRequired().HasMaxLength(50);
        builder.Property(e => e.ParamSchema).HasColumnName("param_schema")
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValue(false);
    }
}
