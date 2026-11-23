using NeoOrders.Application.DTOs.Sellers;

namespace NeoOrders.Application.Contracts.Sellers;

public interface ISellerService
{
    Task<List<SellerDto>> ListAsync(bool? onlyActive);
    Task<SellerDto?> GetByIdAsync(int id);
    Task<SellerDto> CreateAsync(SellerDto dto);
    Task<bool> UpdateAsync(int id, SellerDto dto);
    Task<(bool Ok, string? Error)> DeleteAsync(int id);
}
