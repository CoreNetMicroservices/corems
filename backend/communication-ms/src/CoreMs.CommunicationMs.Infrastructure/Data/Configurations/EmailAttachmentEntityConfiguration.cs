using CoreMs.CommunicationMs.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreMs.CommunicationMs.Infrastructure.Data.Configurations;

public class EmailAttachmentEntityConfiguration : IEntityTypeConfiguration<EmailAttachmentEntity>
{
    public void Configure(EntityTypeBuilder<EmailAttachmentEntity> builder)
    {
        builder.ToTable("email_attachment");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").UseIdentityAlwaysColumn();

        builder.HasIndex(e => e.EmailMessageId).HasDatabaseName("idx_email_attachment_email");

        builder.Property(e => e.EmailMessageId).HasColumnName("email_message_id").IsRequired();
        builder.Property(e => e.DocumentUuid).HasColumnName("document_uuid").IsRequired();
        builder.Property(e => e.Checksum).HasColumnName("checksum").HasMaxLength(255);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
