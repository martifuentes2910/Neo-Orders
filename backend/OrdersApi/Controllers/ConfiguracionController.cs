using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersApi.Data;
using OrdersApi.Models;

namespace OrdersApi.Controllers;

[ApiController]
[Route("api/config")]
[Authorize]
public class ConfiguracionController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    public ConfiguracionController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // Company settings (admin)
    [HttpGet("empresa")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetEmpresa()
    {
        var s = await _db.CompanySettings.FirstOrDefaultAsync();
        if (s == null)
        {
            s = new CompanySettings();
            _db.CompanySettings.Add(s);
            await _db.SaveChangesAsync();
        }
        return Ok(s);
    }

    [HttpPut("empresa")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateEmpresa([FromBody] CompanySettings dto)
    {
        var s = await _db.CompanySettings.FirstOrDefaultAsync();
        if (s == null) { s = new CompanySettings(); _db.CompanySettings.Add(s); }
        s.NombreEmpresa = dto.NombreEmpresa;
        s.Moneda = dto.Moneda;
        s.TipoDeCambio = dto.TipoDeCambio;
        await _db.SaveChangesAsync();
        return Ok(s);
    }

    // Profile (current user)
    [HttpGet("perfil")]
    public async Task<IActionResult> GetPerfil()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();
        return Ok(new { user.Id, user.Email, user.Nombre, user.Apellido, user.Avatar, user.Tema });
    }

    [HttpPut("perfil")]
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

    // Seed data (admin)
    [HttpPost("seed")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Seed()
    {
        // Re-run seeder (idempotent for roles/users/settings; clients/orders only if empty)
        await DbSeeder.SeedAsync(HttpContext.RequestServices);
        return Ok(new { message = "Datos de ejemplo verificados/cargados" });
    }
}

public class PerfilDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Avatar { get; set; } = "avatar-1";
    public string Tema { get; set; } = "claro";
}
