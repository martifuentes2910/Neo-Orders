using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NeoOrders.Infrastructure.Data;
using NeoOrders.Infrastructure.Models;

using NeoOrders.Application.DTOs.Users;

namespace NeoOrders.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsuariosController : ControllerBase
{
    private const string AdminRole = "Admin";
    private const string EmployeeRole = "Empleado";
    private static readonly string[] AllowedRoles = [AdminRole, EmployeeRole];

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    public UsuariosController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    /// <summary>
    /// KAN-39: identifica al usuario autenticado para impedir operaciones sobre uno mismo.
    /// </summary>
    private bool IsSelf(string id) =>
        string.Equals(_userManager.GetUserId(User), id, StringComparison.Ordinal);

    /// <summary>
    /// KAN-39: true cuando el usuario tiene el rol Admin y es el único en el sistema.
    /// </summary>
    private async Task<bool> IsOnlyAdminAsync(ApplicationUser user, IList<string>? currentRoles = null)
    {
        currentRoles ??= await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(AdminRole, StringComparer.Ordinal)) return false;

        var admins = await _userManager.GetUsersInRoleAsync(AdminRole);
        return admins.Count == 1 && string.Equals(admins[0].Id, user.Id, StringComparison.Ordinal);
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
        var result = new List<object>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            result.Add(new
            {
                u.Id,
                u.Email,
                u.Nombre,
                u.Apellido,
                u.Avatar,
                Roles = roles
            });
        }
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CrearUsuarioDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (await _userManager.FindByEmailAsync(dto.Email) != null)
            return BadRequest(new { message = "El email ya está registrado" });

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            Nombre = dto.Nombre,
            Apellido = dto.Apellido,
            Avatar = dto.Avatar ?? "avatar-1",
            Tema = "claro",
            EmailConfirmed = true
        };
        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToArray();
            return BadRequest(new { message = string.Join(" ", errors), errors });
        }
        await _userManager.AddToRoleAsync(user, dto.Rol);
        return Ok(new { user.Id, user.Email });
    }

    [HttpPut("{id}/rol")]
    public async Task<IActionResult> SetRol(string id, [FromBody] RolDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(new { message = "Usuario no encontrado." });
        // Sin esta guardia se removían todos los roles y AddToRoleAsync fallaba
        // silenciosamente, dejando al usuario sin ningún rol (KAN-41).
        // KAN-39: además, el rol debe ser uno de los definidos en el dominio.
        if (string.IsNullOrWhiteSpace(dto.Rol) ||
            !AllowedRoles.Contains(dto.Rol, StringComparer.Ordinal) ||
            !await _roleManager.RoleExistsAsync(dto.Rol))
            return BadRequest(new { message = "Rol no válido." });

        // KAN-39: el administrador no debe auto-modificarse el rol: los claims de
        // la cookie siguen vigentes hasta el próximo login (autorización obsoleta).
        if (IsSelf(id))
            return BadRequest(new { message = "No puedes cambiar tu propio rol." });

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains(AdminRole, StringComparer.Ordinal) &&
            !string.Equals(dto.Rol, AdminRole, StringComparison.Ordinal) &&
            await IsOnlyAdminAsync(user, roles))
            return BadRequest(new { message = "No se puede quitar el rol de administrador al último administrador." });

        await _userManager.RemoveFromRolesAsync(user, roles);
        await _userManager.AddToRoleAsync(user, dto.Rol);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(new { message = "Usuario no encontrado." });

        // KAN-39: no auto-eliminarse (la sesión quedaría con claims huérfanos) y
        // no eliminar al último Admin (dejaría el sistema sin administradores).
        if (IsSelf(id))
            return BadRequest(new { message = "No puedes eliminar tu propio usuario." });

        if (await IsOnlyAdminAsync(user))
            return BadRequest(new { message = "No se puede eliminar al último administrador." });

        await _userManager.DeleteAsync(user);
        return NoContent();
    }
}
