using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NeoOrders.Application.Contracts.Config;
using NeoOrders.Application.DTOs.Config;
using NeoOrders.Infrastructure.Models;

namespace NeoOrders.Api.Controllers;

[ApiController]
[Route("api/config")]
[Authorize]
public class ConfiguracionController : ControllerBase
{
    private readonly IConfigService _config;
    private readonly UserManager<ApplicationUser> _userManager;
    public ConfiguracionController(IConfigService config, UserManager<ApplicationUser> userManager)
    {
        _config = config;
        _userManager = userManager;
    }

    [HttpGet("company")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetEmpresa() => Ok(await _config.GetCompanyAsync());

    [HttpPut("company")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateEmpresa([FromBody] CompanySettingsDto dto) => Ok(await _config.UpdateCompanyAsync(dto));

    [HttpGet("profile")]
    public async Task<IActionResult> GetPerfil()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();
        return Ok(new { user.Id, user.Email, user.Nombre, user.Apellido, user.Avatar, user.Tema });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdatePerfil([FromBody] PerfilDto dto)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();
        user.Nombre = dto.Nombre;
        user.Apellido = dto.Apellido;
        user.Avatar = dto.Avatar;
        user.Tema = dto.Tema;
        await _userManager.UpdateAsync(user);
        return Ok(new { user.Id, user.Email, user.Nombre, user.Apellido, user.Avatar, user.Tema });
    }

    [HttpPost("seed")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Seed()
    {
        await _config.SeedAsync(HttpContext.RequestServices);
        return Ok(new { message = "Datos de ejemplo verificados/cargados" });
    }

    [HttpPost("clear")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Clear()
    {
        var result = await _config.ClearAsync();
        return Ok(result);
    }
}
