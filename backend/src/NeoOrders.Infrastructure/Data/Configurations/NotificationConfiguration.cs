using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeoOrders.Domain.Notifications;

namespace NeoOrders.Infrastructure.Data.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> entity)
    {
        entity.Property(n => n.Tipo).IsRequired().HasMaxLength(30);
        entity.Property(n => n.Accion).IsRequired().HasMaxLength(30);
        entity.Property(n => n.Titulo).IsRequired().HasMaxLength(160);
        entity.Property(n => n.Mensaje).IsRequired().HasMaxLength(400);
        entity.Property(n => n.Link).HasMaxLength(160);
        entity.Property(n => n.Usuario).HasMaxLength(120);
        entity.HasIndex(n => n.Fecha);
    }
}
