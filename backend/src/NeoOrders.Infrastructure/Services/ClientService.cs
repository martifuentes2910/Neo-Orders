using Microsoft.EntityFrameworkCore;
using NeoOrders.Application.Contracts.Clients;
using NeoOrders.Application.DTOs.Clients;
using NeoOrders.Infrastructure.Data;
using NeoOrders.Infrastructure.Models;

namespace NeoOrders.Infrastructure.Services;

public class ClientService : IClientService
{
    private readonly AppDbContext _db;
    public ClientService(AppDbContext db) => _db = db;

    public async Task<List<ClientListItemDto>> ListAsync(string? search)
    {
        var q = _db.Clients.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(c => c.Nombre.ToLower().Contains(s) || c.Email.ToLower().Contains(s));
        }
        return await q.OrderBy(c => c.Nombre).Select(c => new ClientListItemDto
        {
            Id = c.Id,
            Nombre = c.Nombre,
            Email = c.Email,
            Telefono = c.Telefono,
            Direccion = c.Direccion,
            FechaAlta = c.FechaAlta,
            OrdersCount = c.Orders.Count,
            TotalSpent = c.Orders.Sum(o => (decimal?)o.Total) ?? 0m
        }).ToListAsync();
    }

    public async Task<ClientDto?> GetByIdAsync(int id)
    {
        return await _db.Clients.AsNoTracking().Where(c => c.Id == id)
            .Select(c => new ClientDto { Id = c.Id, Nombre = c.Nombre, Email = c.Email, Telefono = c.Telefono, Direccion = c.Direccion, FechaAlta = c.FechaAlta })
            .FirstOrDefaultAsync();
    }

    public async Task<ClientDto> CreateAsync(ClientDto dto)
    {
        var entity = new Client { Nombre = dto.Nombre, Email = dto.Email, Telefono = dto.Telefono, Direccion = dto.Direccion };
        _db.Clients.Add(entity);
        await _db.SaveChangesAsync();
        dto.Id = entity.Id;
        dto.FechaAlta = entity.FechaAlta;
        return dto;
    }

    public async Task<bool> UpdateAsync(int id, ClientDto dto)
    {
        var existing = await _db.Clients.FindAsync(id);
        if (existing == null) return false;
        existing.Nombre = dto.Nombre;
        existing.Email = dto.Email;
        existing.Telefono = dto.Telefono;
        existing.Direccion = dto.Direccion;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Ok, string? Error)> DeleteAsync(int id)
    {
        var c = await _db.Clients.Include(x => x.Orders).FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return (false, "NOT_FOUND");
        if (c.Orders.Any()) return (false, "El cliente tiene pedidos asociados");
        _db.Clients.Remove(c);
        await _db.SaveChangesAsync();
        return (true, null);
    }
}
