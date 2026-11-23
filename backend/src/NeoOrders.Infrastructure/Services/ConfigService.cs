using Microsoft.EntityFrameworkCore;
using NeoOrders.Application.Contracts.Config;
using NeoOrders.Application.DTOs.Config;
using NeoOrders.Infrastructure.Data;
using NeoOrders.Infrastructure.Models;

namespace NeoOrders.Infrastructure.Services;

public class ConfigService : IConfigService
{
    private readonly AppDbContext _db;
    public ConfigService(AppDbContext db) => _db = db;

    public async Task<CompanySettingsDto> GetCompanyAsync()
    {
        var s = await _db.CompanySettings.FirstOrDefaultAsync();
        if (s == null) { s = new CompanySettings(); _db.CompanySettings.Add(s); await _db.SaveChangesAsync(); }
        return Map(s);
    }

    public async Task<CompanySettingsDto> UpdateCompanyAsync(CompanySettingsDto dto)
    {
        var s = await _db.CompanySettings.FirstOrDefaultAsync();
        if (s == null) { s = new CompanySettings(); _db.CompanySettings.Add(s); }
        s.NombreEmpresa = dto.NombreEmpresa;
        s.Moneda = dto.Moneda;
        s.TipoDeCambio = dto.TipoDeCambio;
        await _db.SaveChangesAsync();
        return Map(s);
    }

    public Task SeedAsync(IServiceProvider sp) => DbSeeder.SeedAsync(sp);

    private static CompanySettingsDto Map(CompanySettings s) => new()
    { Id = s.Id, NombreEmpresa = s.NombreEmpresa, Moneda = s.Moneda, TipoDeCambio = s.TipoDeCambio };
}
