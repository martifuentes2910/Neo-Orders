using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OrdersApi.Models;

namespace OrdersApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpPost("login")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null) return Unauthorized(new { message = "Credenciales inválidas" });

        var result = await _signInManager.PasswordSignInAsync(user, dto.Password, isPersistent: true, lockoutOnFailure: false);
        if (!result.Succeeded) return Unauthorized(new { message = "Credenciales inválidas" });

        return Ok(await ToDtoAsync(user));
    }

    [HttpPost("logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        if (User?.Identity?.IsAuthenticated != true) return Unauthorized();
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();
        return Ok(await ToDtoAsync(user));
    }

    private async Task<UserDto> ToDtoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Nombre = user.Nombre,
            Apellido = user.Apellido,
            Avatar = user.Avatar,
            Tema = user.Tema,
            Roles = roles.ToList()
        };
    }
}

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string Tema { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}
