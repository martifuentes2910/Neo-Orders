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

    public async Task<object> ClearAsync()
    {
        var notifications = await _db.Notifications.CountAsync();
        var orders = await _db.Orders.CountAsync();
        var products = await _db.Products.CountAsync();
        var sellers = await _db.Sellers.CountAsync();
        var clients = await _db.Clients.CountAsync();

        _db.Notifications.RemoveRange(_db.Notifications);
        _db.Orders.RemoveRange(_db.Orders);
        _db.Products.RemoveRange(_db.Products);
        _db.Sellers.RemoveRange(_db.Sellers);
        _db.Clients.RemoveRange(_db.Clients);
        await _db.SaveChangesAsync();

        return new
        {
            message = "Datos operativos eliminados",
            eliminados = new { notificaciones = notifications, pedidos = orders, productos = products, vendedores = sellers, clientes = clients }
        };
    }

    private static CompanySettingsDto Map(CompanySettings s) => new()
    { Id = s.Id, NombreEmpresa = s.NombreEmpresa, Moneda = s.Moneda, TipoDeCambio = s.TipoDeCambio };
}
