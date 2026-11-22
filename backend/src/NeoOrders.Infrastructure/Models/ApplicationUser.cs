using Microsoft.AspNetCore.Identity;

namespace OrdersApi.Models;

public class ApplicationUser : IdentityUser
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Avatar { get; set; } = "avatar-1";
    public string Tema { get; set; } = "claro";
    public string Paleta { get; set; } = "coral";
}
