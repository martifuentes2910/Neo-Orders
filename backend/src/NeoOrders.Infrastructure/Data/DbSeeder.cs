using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrdersApi.Models;

namespace OrdersApi.Data;

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
            db.CompanySettings.Add(new CompanySettings
            {
                NombreEmpresa = "NeoOrders S.A.",
                Moneda = "ARS",
                TipoDeCambio = 1m
            });
            await db.SaveChangesAsync();
        }

        if (!await db.Clients.AnyAsync())
        {
            var clients = new List<Client>
            {
                new() { Nombre = "Juan Pérez", Email = "juan.perez@mail.com", Telefono = "+54 11 4000-1001", Direccion = "Av. Corrientes 1234, CABA", FechaAlta = DateTime.UtcNow.AddMonths(-6) },
                new() { Nombre = "María López", Email = "maria.lopez@mail.com", Telefono = "+54 11 4000-1002", Direccion = "Calle Falsa 123, La Plata", FechaAlta = DateTime.UtcNow.AddMonths(-5) },
                new() { Nombre = "Carlos García", Email = "carlos.garcia@mail.com", Telefono = "+54 351 4000-1003", Direccion = "Bv. San Juan 456, Córdoba", FechaAlta = DateTime.UtcNow.AddMonths(-4) },
                new() { Nombre = "Lucía Fernández", Email = "lucia.fernandez@mail.com", Telefono = "+54 261 4000-1004", Direccion = "San Martín 789, Mendoza", FechaAlta = DateTime.UtcNow.AddMonths(-4) },
                new() { Nombre = "Martín Rodríguez", Email = "martin.rodriguez@mail.com", Telefono = "+54 381 4000-1005", Direccion = "25 de Mayo 321, Tucumán", FechaAlta = DateTime.UtcNow.AddMonths(-3) },
                new() { Nombre = "Sofía Martínez", Email = "sofia.martinez@mail.com", Telefono = "+54 223 4000-1006", Direccion = "Av. Colón 654, Mar del Plata", FechaAlta = DateTime.UtcNow.AddMonths(-3) },
                new() { Nombre = "Diego Sánchez", Email = "diego.sanchez@mail.com", Telefono = "+54 342 4000-1007", Direccion = "San Lorenzo 987, Santa Fe", FechaAlta = DateTime.UtcNow.AddMonths(-2) },
                new() { Nombre = "Valentina Torres", Email = "valentina.torres@mail.com", Telefono = "+54 299 4000-1008", Direccion = "Roca 147, Neuquén", FechaAlta = DateTime.UtcNow.AddMonths(-2) },
                new() { Nombre = "Andrés Ramírez", Email = "andres.ramirez@mail.com", Telefono = "+54 2966 4000-1009", Direccion = "San Martín 258, Ushuaia", FechaAlta = DateTime.UtcNow.AddMonths(-1) },
                new() { Nombre = "Camila Díaz", Email = "camila.diaz@mail.com", Telefono = "+54 379 4000-1010", Direccion = "Junín 369, Corrientes", FechaAlta = DateTime.UtcNow.AddMonths(-1) },
            };
            db.Clients.AddRange(clients);
            await db.SaveChangesAsync();

            var sellers = new List<Seller>
            {
                new() { Name = "Ana Gómez", Email = "ana.gomez@neoorders.com", Phone = "+54 11 5000-2001", CommissionPercentage = 5.0m, Active = true },
                new() { Name = "Bruno Díaz", Email = "bruno.diaz@neoorders.com", Phone = "+54 11 5000-2002", CommissionPercentage = 6.5m, Active = true },
                new() { Name = "Carla Ruiz", Email = "carla.ruiz@neoorders.com", Phone = "+54 11 5000-2003", CommissionPercentage = 4.0m, Active = true },
                new() { Name = "Diego Fernández", Email = "diego.fernandez@neoorders.com", Phone = "+54 11 5000-2004", CommissionPercentage = 7.0m, Active = true },
                new() { Name = "Elena Torres", Email = "elena.torres@neoorders.com", Phone = "+54 11 5000-2005", CommissionPercentage = 5.5m, Active = true },
            };
            db.Sellers.AddRange(sellers);
            await db.SaveChangesAsync();

            var products = new List<Product>
            {
                new() { Sku = "NB-PRO-15", Name = "Notebook Pro 15", Category = "Equipos", BasePrice = 450000m, Cost = 360000m, Stock = 12, MinStockAlert = 5 },
                new() { Sku = "MOU-WL-01", Name = "Mouse inalámbrico", Category = "Periféricos", BasePrice = 12000m, Cost = 8000m, Stock = 45, MinStockAlert = 10 },
                new() { Sku = "KEY-MEC-01", Name = "Teclado mecánico", Category = "Periféricos", BasePrice = 35000m, Cost = 25000m, Stock = 18, MinStockAlert = 5 },
                new() { Sku = "MON-27-01", Name = "Monitor 27\"", Category = "Equipos", BasePrice = 180000m, Cost = 140000m, Stock = 9, MinStockAlert = 3 },
                new() { Sku = "AUD-BT-01", Name = "Auriculares Bluetooth", Category = "Audio", BasePrice = 28000m, Cost = 20000m, Stock = 25, MinStockAlert = 8 },
                new() { Sku = "WEB-HD-01", Name = "Webcam HD", Category = "Periféricos", BasePrice = 22000m, Cost = 15000m, Stock = 14, MinStockAlert = 5 },
                new() { Sku = "SSD-1TB-01", Name = "Disco SSD 1TB", Category = "Almacenamiento", BasePrice = 55000m, Cost = 40000m, Stock = 30, MinStockAlert = 10 },
                new() { Sku = "RAM-16GB-01", Name = "Memoria RAM 16GB", Category = "Componentes", BasePrice = 48000m, Cost = 35000m, Stock = 22, MinStockAlert = 6 },
                new() { Sku = "SIL-ERG-01", Name = "Silla ergonómica", Category = "Mobiliario", BasePrice = 95000m, Cost = 70000m, Stock = 7, MinStockAlert = 3 },
                new() { Sku = "ESC-AJU-01", Name = "Escritorio ajustable", Category = "Mobiliario", BasePrice = 120000m, Cost = 90000m, Stock = 4, MinStockAlert = 2 },
            };
            db.Products.AddRange(products);
            await db.SaveChangesAsync();

            var rnd = new Random(42);
            var estados = OrderStatuses.All;
            var pedidos = new List<Order>();
            for (int i = 0; i < 60; i++)
            {
                var cliente = clients[rnd.Next(clients.Count)];
                var seller = sellers[rnd.Next(sellers.Count)];
                var product = products[rnd.Next(products.Count)];
                var cantidad = rnd.Next(1, 4);
                var fecha = DateTime.UtcNow.AddDays(-rnd.Next(0, 180));
                var estado = estados[rnd.Next(estados.Length)];
                if (i % 7 == 0) estado = "Entregado";
                if (i % 11 == 0) estado = "Pendiente";
                pedidos.Add(new Order
                {
                    ClientId = cliente.Id,
                    SellerId = seller.Id,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Cantidad = cantidad,
                    PrecioUnitario = product.BasePrice,
                    Total = cantidad * product.BasePrice,
                    Fecha = fecha,
                    Estado = estado
                });
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
