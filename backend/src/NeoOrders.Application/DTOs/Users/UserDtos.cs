namespace NeoOrders.Application.DTOs.Users;

public class CrearUsuarioDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Rol { get; set; } = "Empleado";
    public string? Avatar { get; set; } = "avatar-1";
}

public class RolDto
{
    public string Rol { get; set; } = string.Empty;
}
