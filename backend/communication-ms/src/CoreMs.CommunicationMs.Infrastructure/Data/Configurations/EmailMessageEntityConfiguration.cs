using CoreMs.CommunicationMs.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreMs.CommunicationMs.Infrastructure.Data.Configurations;

public class EmailMessageEntityConfiguration : IEntityTypeConfiguration<EmailMessageEntity>
{
    public void Configure(EntityTypeBuilder<EmailMessageEntity> builder)
    {
        builder.ToTable("email");

        builder.Property(e => e.EmailType).HasColumnName("email_type").IsRequired().HasMaxLength(255);
        builder.Property(e => e.Subject).HasColumnName("subject").IsRequired().HasMaxLength(255);
        builder.Property(e => e.Sender).HasColumnName("sender").IsRequired().HasMaxLength(255);
        builder.Property(e => e.SenderName).HasColumnName("sender_name").HasMaxLength(255);
        builder.Property(e => e.Cc).HasColumnName("cc").HasMaxLength(255);
        builder.Property(e => e.Bcc).HasColumnName("bcc").HasMaxLength(255);
        builder.Property(e => e.Recipient).HasColumnName("recipient").IsRequired().HasMaxLength(255);
        builder.Property(e => e.Body).HasColumnName("body").IsRequired().HasColumnType("text");

        builder.HasMany(e => e.Attachments)
            .WithOne(a => a.EmailMessage)
            .HasForeignKey(a => a.EmailMessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
