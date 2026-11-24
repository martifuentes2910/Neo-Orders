using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NeoOrders.Infrastructure.Models;
using NeoOrders.Domain.Clients;
using NeoOrders.Domain.Config;
using NeoOrders.Domain.Orders;
using NeoOrders.Domain.Products;
using NeoOrders.Domain.Sellers;

namespace NeoOrders.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<AppDbContext>();

        foreach (var role in new[] { "Admin", "Empleado" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        await EnsureUserAsync(userManager, "admin@neoorders.com", "Admin123!", "admin", "Administrador", "Sistema", "avatar-1", "claro", "Admin");
        await EnsureUserAsync(userManager, "empleado@neoorders.com", "Empleado123!", "empleado", "María", "Gómez", "avatar-2", "claro", "Empleado");

        if (!await db.CompanySettings.AnyAsync())
        {
            db.CompanySettings.Add(CompanySettings.Create("NeoOrders S.A.", "ARS", 1m));
            await db.SaveChangesAsync();
        }

        if (!await db.Clients.AnyAsync())
        {
            var clients = new List<Client>
            {
                Client.Create("Juan Pérez", "juan.perez@mail.com", "+54 11 4000-1001", "Av. Corrientes 1234, CABA", DateTime.UtcNow.AddMonths(-6)),
                Client.Create("María López", "maria.lopez@mail.com", "+54 11 4000-1002", "Calle Falsa 123, La Plata", DateTime.UtcNow.AddMonths(-5)),
                Client.Create("Carlos García", "carlos.garcia@mail.com", "+54 351 4000-1003", "Bv. San Juan 456, Córdoba", DateTime.UtcNow.AddMonths(-4)),
                Client.Create("Lucía Fernández", "lucia.fernandez@mail.com", "+54 261 4000-1004", "San Martín 789, Mendoza", DateTime.UtcNow.AddMonths(-4)),
                Client.Create("Martín Rodríguez", "martin.rodriguez@mail.com", "+54 381 4000-1005", "25 de Mayo 321, Tucumán", DateTime.UtcNow.AddMonths(-3)),
                Client.Create("Sofía Martínez", "sofia.martinez@mail.com", "+54 223 4000-1006", "Av. Colón 654, Mar del Plata", DateTime.UtcNow.AddMonths(-3)),
                Client.Create("Diego Sánchez", "diego.sanchez@mail.com", "+54 342 4000-1007", "San Lorenzo 987, Santa Fe", DateTime.UtcNow.AddMonths(-2)),
                Client.Create("Valentina Torres", "valentina.torres@mail.com", "+54 299 4000-1008", "Roca 147, Neuquén", DateTime.UtcNow.AddMonths(-2)),
                Client.Create("Andrés Ramírez", "andres.ramirez@mail.com", "+54 2966 4000-1009", "San Martín 258, Ushuaia", DateTime.UtcNow.AddMonths(-1)),
                Client.Create("Camila Díaz", "camila.diaz@mail.com", "+54 379 4000-1010", "Junín 369, Corrientes", DateTime.UtcNow.AddMonths(-1)),
            };
            db.Clients.AddRange(clients);
            await db.SaveChangesAsync();

            var sellers = new List<Seller>
            {
                Seller.Create("Ana Gómez", "ana.gomez@neoorders.com", "+54 11 5000-2001", 5.0m),
                Seller.Create("Bruno Díaz", "bruno.diaz@neoorders.com", "+54 11 5000-2002", 6.5m),
                Seller.Create("Carla Ruiz", "carla.ruiz@neoorders.com", "+54 11 5000-2003", 4.0m),
                Seller.Create("Diego Fernández", "diego.fernandez@neoorders.com", "+54 11 5000-2004", 7.0m),
                Seller.Create("Elena Torres", "elena.torres@neoorders.com", "+54 11 5000-2005", 5.5m),
            };
            db.Sellers.AddRange(sellers);
            await db.SaveChangesAsync();

            var products = new List<Product>
            {
                Product.Create("NB-PRO-15", "Notebook Pro 15", "Equipos", 450000m, 360000m, 12, 5),
                Product.Create("MOU-WL-01", "Mouse inalámbrico", "Periféricos", 12000m, 8000m, 45, 10),
                Product.Create("KEY-MEC-01", "Teclado mecánico", "Periféricos", 35000m, 25000m, 18, 5),
                Product.Create("MON-27-01", "Monitor 27\"", "Equipos", 180000m, 140000m, 9, 3),
                Product.Create("AUD-BT-01", "Auriculares Bluetooth", "Audio", 28000m, 20000m, 25, 8),
                Product.Create("WEB-HD-01", "Webcam HD", "Periféricos", 22000m, 15000m, 14, 5),
                Product.Create("SSD-1TB-01", "Disco SSD 1TB", "Almacenamiento", 55000m, 40000m, 30, 10),
                Product.Create("RAM-16GB-01", "Memoria RAM 16GB", "Componentes", 48000m, 35000m, 22, 6),
                Product.Create("SIL-ERG-01", "Silla ergonómica", "Mobiliario", 95000m, 70000m, 7, 3),
                Product.Create("ESC-AJU-01", "Escritorio ajustable", "Mobiliario", 120000m, 90000m, 4, 2),
            };
            db.Products.AddRange(products);
            await db.SaveChangesAsync();

            var rnd = new Random(42);
            var estados = OrderStatusPolicy.All;
            var pedidos = new List<Order>();
            for (int i = 0; i < 60; i++)
            {
                var cliente = clients[rnd.Next(clients.Count)];
                var seller = sellers[rnd.Next(sellers.Count)];
                var product = products[rnd.Next(products.Count)];
                var cantidad = rnd.Next(1, 4);
                var fecha = DateTime.UtcNow.AddDays(-rnd.Next(0, 180));
                var estado = estados[rnd.Next(estados.Length)];
                if (i % 7 == 0) estado = OrderStatusPolicy.Entregado;
                if (i % 11 == 0) estado = OrderStatusPolicy.Pendiente;
                pedidos.Add(Order.Rehydrate(
                    id: 0,
                    clientId: cliente.Id,
                    sellerId: seller.Id,
                    productId: product.Id,
                    productName: product.Name,
                    cantidad: cantidad,
                    precioUnitario: product.BasePrice,
                    total: cantidad * product.BasePrice,
                    fecha: fecha,
                    estado: estado,
                    orderNumber: null));
            }
            db.Orders.AddRange(pedidos);
            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureUserAsync(UserManager<ApplicationUser> userManager, string email, string password,
        string userName, string nombre, string apellido, string avatar, string tema, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                Nombre = nombre,
                Apellido = apellido,
                Avatar = avatar,
                Tema = tema,
                Paleta = "coral",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
        else
        {
            user.UserName = userName;
            user.Nombre = nombre;
            user.Apellido = apellido;
            user.Avatar = avatar;
            user.Tema = tema;
            if (string.IsNullOrWhiteSpace(user.Paleta)) user.Paleta = "coral";
            await userManager.UpdateAsync(user);
            if (!await userManager.IsInRoleAsync(user, role))
                await userManager.AddToRoleAsync(user, role);
        }
    }
}
