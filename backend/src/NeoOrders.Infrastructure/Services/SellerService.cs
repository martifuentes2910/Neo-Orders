using Microsoft.EntityFrameworkCore;
using NeoOrders.Application.Contracts.Sellers;
using NeoOrders.Application.DTOs.Sellers;
using NeoOrders.Infrastructure.Data;
using NeoOrders.Infrastructure.Models;

namespace NeoOrders.Infrastructure.Services;

public class SellerService : ISellerService
{
    private readonly AppDbContext _db;
    public SellerService(AppDbContext db) => _db = db;

    public async Task<List<SellerDto>> ListAsync(bool? onlyActive)
    {
        var q = _db.Sellers.AsNoTracking().AsQueryable();
        if (onlyActive == true) q = q.Where(s => s.Active);
        return await q.OrderBy(s => s.Name).Select(s => new SellerDto
        {
            Id = s.Id, Name = s.Name, Email = s.Email, Phone = s.Phone, CommissionPercentage = s.CommissionPercentage, Active = s.Active
        }).ToListAsync();
    }

    public Task<SellerDto?> GetByIdAsync(int id) =>
        _db.Sellers.AsNoTracking().Where(s => s.Id == id)
            .Select(s => new SellerDto { Id = s.Id, Name = s.Name, Email = s.Email, Phone = s.Phone, CommissionPercentage = s.CommissionPercentage, Active = s.Active })
            .FirstOrDefaultAsync();

    public async Task<SellerDto> CreateAsync(SellerDto dto)
    {
        var entity = new Seller { Name = dto.Name, Email = dto.Email, Phone = dto.Phone, CommissionPercentage = dto.CommissionPercentage, Active = dto.Active };
        _db.Sellers.Add(entity);
        await _db.SaveChangesAsync();
        dto.Id = entity.Id;
        return dto;
    }

    public async Task<bool> UpdateAsync(int id, SellerDto dto)
    {
        var existing = await _db.Sellers.FindAsync(id);
        if (existing == null) return false;
        existing.Name = dto.Name; existing.Email = dto.Email; existing.Phone = dto.Phone;
        existing.CommissionPercentage = dto.CommissionPercentage; existing.Active = dto.Active;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Ok, string? Error)> DeleteAsync(int id)
    {
        var s = await _db.Sellers.Include(x => x.Orders).FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return (false, "NOT_FOUND");
        if (s.Orders.Any()) return (false, "El vendedor tiene pedidos asociados");
        _db.Sellers.Remove(s);
        await _db.SaveChangesAsync();
        return (true, null);
    }
}
