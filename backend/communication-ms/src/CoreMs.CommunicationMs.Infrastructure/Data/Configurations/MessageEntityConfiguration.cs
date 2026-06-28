using CoreMs.CommunicationMs.Core.Entities;
using CoreMs.CommunicationMs.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreMs.CommunicationMs.Infrastructure.Data.Configurations;

public class MessageEntityConfiguration : IEntityTypeConfiguration<MessageEntity>
{
    public void Configure(EntityTypeBuilder<MessageEntity> builder)
    {
        builder.ToTable("message");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();

        builder.UseTptMappingStrategy();

        builder.HasIndex(e => e.Uuid).IsUnique();
        builder.HasIndex(e => e.UserId).HasDatabaseName("idx_message_user");
        builder.HasIndex(e => e.Type).HasDatabaseName("idx_message_type");
        builder.HasIndex(e => e.Status).HasDatabaseName("idx_message_status");
        builder.HasIndex(e => e.CreatedAt).HasDatabaseName("idx_message_created_at");

        builder.Property(e => e.Uuid).HasColumnName("uuid").IsRequired();
        builder.Property(e => e.Type).HasColumnName("type").IsRequired()
            .HasConversion<string>().HasMaxLength(31);
        builder.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(e => e.Status).HasColumnName("status").IsRequired()
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.SentAt).HasColumnName("sent_at");
        builder.Property(e => e.SentByType).HasColumnName("sent_by_type")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.SentById).HasColumnName("sent_by_id");
    }
}
