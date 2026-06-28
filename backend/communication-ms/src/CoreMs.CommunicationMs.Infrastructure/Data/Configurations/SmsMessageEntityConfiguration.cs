using CoreMs.CommunicationMs.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreMs.CommunicationMs.Infrastructure.Data.Configurations;

public class SmsMessageEntityConfiguration : IEntityTypeConfiguration<SmsMessageEntity>
{
    public void Configure(EntityTypeBuilder<SmsMessageEntity> builder)
    {
        builder.ToTable("sms");

        builder.Property(e => e.PhoneNumber).HasColumnName("phone_number").IsRequired().HasMaxLength(50);
        builder.Property(e => e.Message).HasColumnName("message").IsRequired().HasColumnType("text");
        builder.Property(e => e.Sid).HasColumnName("sid").HasMaxLength(255);
    }
}
