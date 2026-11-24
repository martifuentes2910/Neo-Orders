using Microsoft.EntityFrameworkCore;
using NeoOrders.Application.Contracts.Sellers;
using NeoOrders.Application.DTOs.Sellers;
using NeoOrders.Infrastructure.Data;

namespace NeoOrders.Infrastructure.Services;

public class SellerService : ISellerService
{
    private readonly AppDbContext _db;
    public SellerService(AppDbContext db) => _db = db;

    private static bool IsUniqueConstraint(DbUpdateException ex)
        => ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true
           || ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true;

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
        var email = dto.Email.Trim().ToLowerInvariant();
        var entity = new Seller { Name = dto.Name, Email = email, Phone = dto.Phone, CommissionPercentage = dto.CommissionPercentage, Active = dto.Active };
        _db.Sellers.Add(entity);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraint(ex))
        {
            throw new InvalidOperationException("Ya existe un vendedor con ese email.");
        }
        dto.Id = entity.Id;
        dto.Email = email;
        return dto;
    }

    public async Task<bool> UpdateAsync(int id, SellerDto dto)
    {
        var existing = await _db.Sellers.FindAsync(id);
        if (existing == null) return false;
        existing.Name = dto.Name; existing.Email = dto.Email.Trim().ToLowerInvariant(); existing.Phone = dto.Phone;
        existing.CommissionPercentage = dto.CommissionPercentage; existing.Active = dto.Active;
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraint(ex))
        {
            throw new InvalidOperationException("Ya existe un vendedor con ese email.");
        }
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
