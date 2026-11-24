using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeoOrders.Domain.Clients;

namespace NeoOrders.Infrastructure.Data.Configurations;

public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> entity)
    {
        entity.Property(c => c.Nombre).IsRequired().HasMaxLength(100);
        entity.Property(c => c.Email).IsRequired();
        entity.Property(c => c.Telefono).HasMaxLength(30);
        entity.Property(c => c.Direccion).HasMaxLength(200);
        entity.HasIndex(c => c.Email).IsUnique();
    }
}
