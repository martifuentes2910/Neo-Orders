using NeoOrders.Application.DTOs.Config;

namespace NeoOrders.Application.Contracts.Config;

public interface IConfigService
{
    Task<CompanySettingsDto> GetCompanyAsync();
    Task<CompanySettingsDto> UpdateCompanyAsync(CompanySettingsDto dto);
    Task SeedAsync(IServiceProvider sp);
}
