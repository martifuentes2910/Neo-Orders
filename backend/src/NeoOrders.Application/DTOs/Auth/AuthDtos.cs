namespace NeoOrders.Application.DTOs.Auth;

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
    public string Rol { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
    public string Avatar { get; set; } = "avatar-1";
    public string Tema { get; set; } = "claro";
    public string Paleta { get; set; } = "coral";
}
