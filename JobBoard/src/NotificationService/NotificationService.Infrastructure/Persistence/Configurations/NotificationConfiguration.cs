using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationService.Domain;

namespace NotificationService.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.RecipientUserId).IsRequired();
        builder.Property(n => n.RecipientRole).IsRequired();
        builder.Property(n => n.Type).IsRequired();
        builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Body).IsRequired().HasMaxLength(1000);
        builder.Property(n => n.Metadata).IsRequired().HasMaxLength(4000);

        builder.HasIndex(n => new { n.RecipientUserId, n.CreatedAtUtc });
        builder.HasIndex(n => new { n.RecipientUserId, n.IsRead });
    }
}
