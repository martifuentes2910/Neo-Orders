using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeoOrders.Domain.Config;

namespace NeoOrders.Infrastructure.Data.Configurations;

public sealed class CompanySettingsConfiguration : IEntityTypeConfiguration<CompanySettings>
{
    public void Configure(EntityTypeBuilder<CompanySettings> entity)
    {
        entity.Property(c => c.NombreEmpresa).IsRequired().HasMaxLength(120);
        entity.Property(c => c.Moneda).IsRequired().HasMaxLength(10);
        entity.Property(c => c.TipoDeCambio).HasPrecision(18, 4);
    }
}
