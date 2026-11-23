using NeoOrders.Application.DTOs.Clients;

namespace NeoOrders.Application.Contracts.Clients;

public interface IClientService
{
    Task<List<ClientListItemDto>> ListAsync(string? search);
    Task<ClientDto?> GetByIdAsync(int id);
    Task<ClientDto> CreateAsync(ClientDto dto);
    Task<bool> UpdateAsync(int id, ClientDto dto);
    Task<(bool Ok, string? Error)> DeleteAsync(int id);
}
