using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NeoOrders.Api.Infrastructure;
using NeoOrders.Application.Contracts.Orders;
using NeoOrders.Application.Contracts.Notifications;
using NeoOrders.Application.Contracts.Import;
using NeoOrders.Application.Contracts.Productos;
using NeoOrders.Application.Contracts.Reportes;
using NeoOrders.Application.Services;
using NeoOrders.Infrastructure.Data;
using NeoOrders.Infrastructure.Data.Repositories.Reportes;
using NeoOrders.Infrastructure.Storage;
using NeoOrders.Infrastructure.Services;
using NeoOrders.Infrastructure.Models;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllersWithViews(options =>
{
    // Validate the antiforgery token on every unsafe request by default.
    // Login/logout opt out with [IgnoreAntiforgeryToken].
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Database=OrdersDb;Trusted_Connection=True;TrustServerCertificate=True;";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sql =>
        sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

// Services
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderQueryService, OrderQueryService>();
builder.Services.AddScoped<NeoOrders.Application.Contracts.Clients.IClientService, ClientService>();
builder.Services.AddScoped<NeoOrders.Application.Contracts.Sellers.ISellerService, SellerService>();
builder.Services.AddScoped<NeoOrders.Application.Contracts.Config.IConfigService, ConfigService>();
builder.Services.AddScoped<NeoOrders.Application.Contracts.Products.IProductService, ProductService>();

// Reportes: contratos en Application, implementaciones en Data (DIP).
builder.Services.AddScoped<IReporteResumenRepository, ReporteResumenRepository>();
builder.Services.AddScoped<IDashboardMetricsRepository, DashboardMetricsRepository>();
builder.Services.AddScoped<IVentasVendedorRepository, VentasVendedorRepository>();
builder.Services.AddScoped<IProductosVendidosRepository, ProductosVendidosRepository>();
builder.Services.AddScoped<IClientesConMasComprasRepository, ClientesConMasComprasRepository>();
builder.Services.AddScoped<IReporteService, ReporteService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IVentasVendedorService, VentasVendedorService>();
builder.Services.AddScoped<IProductosVendidosService, ProductosVendidosService>();
builder.Services.AddScoped<IClientesConMasComprasService, ClientesConMasComprasService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ICsvImportService, CsvImportService>();

// KAN-23: imágenes de producto en Cloudinary. Sin credenciales la app arranca y el CRUD sigue funcionando.
var cloudName = builder.Configuration["Cloudinary:CloudName"];
var cloudApiKey = builder.Configuration["Cloudinary:ApiKey"];
var cloudApiSecret = builder.Configuration["Cloudinary:ApiSecret"];
var cloudFolder = builder.Configuration["Cloudinary:Folder"] ?? "neoorders/products";
var cloudinaryConfigured = !string.IsNullOrWhiteSpace(cloudName)
    && !string.IsNullOrWhiteSpace(cloudApiKey)
    && !string.IsNullOrWhiteSpace(cloudApiSecret);

if (cloudinaryConfigured)
{
    builder.Services.AddSingleton<IProductImageStorage>(sp => new CloudinaryProductImageStorage(
        cloudName!, cloudApiKey!, cloudApiSecret!, cloudFolder,
        sp.GetRequiredService<ILogger<CloudinaryProductImageStorage>>()));
}
else
{
    builder.Services.AddSingleton<IProductImageStorage, UnconfiguredProductImageStorage>();
}

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

var isDev = builder.Environment.IsDevelopment();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "NeoOrders.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = isDev ? SameSiteMode.Lax : SameSiteMode.None;
    options.Cookie.SecurePolicy = isDev ? CookieSecurePolicy.None : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "NeoOrders.XSRF";
    options.Cookie.SameSite = isDev ? SameSiteMode.Lax : SameSiteMode.None;
    options.Cookie.SecurePolicy = isDev ? CookieSecurePolicy.None : CookieSecurePolicy.Always;
});

// CORS
var frontendOrigins = builder.Configuration.GetSection("Frontend:Origins").Get<string[]>();
if (frontendOrigins is null || frontendOrigins.Length == 0)
{
    var csv = builder.Configuration["Frontend:Origins"];
    frontendOrigins = !string.IsNullOrWhiteSpace(csv)
        ? csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        : [
            "http://localhost:5173",
            "https://localhost:5173",
            "http://127.0.0.1:5173",
            "https://127.0.0.1:5173",
            "http://localhost:4173",
            "http://127.0.0.1:4173"
        ];
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(frontendOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

if (!cloudinaryConfigured)
{
    app.Logger.LogWarning(
        "Cloudinary no está configurado. Las imágenes de producto no se pueden cargar hasta definir Cloudinary__CloudName, Cloudinary__ApiKey y Cloudinary__ApiSecret.");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Auto-migrate + seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        db.Database.Migrate();
        var seedOnStartup = builder.Configuration.GetValue<bool?>("Seed:RunOnStartup")
            ?? app.Environment.IsDevelopment();
        if (seedOnStartup)
        {
            await DbSeeder.SeedAsync(scope.ServiceProvider);
            logger.LogInformation("Seed de inicio ejecutado.");
        }
        else
        {
            logger.LogInformation("Seed de inicio omitido por configuración.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error al migrar/sembrar la base de datos.");
    }
}

app.UseExceptionHandler();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapControllers();

// Antiforgery token endpoint
app.MapGet("/api/antiforgery/token", (IAntiforgery antiforgery, HttpContext context) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    return Results.Ok(new { token = tokens.RequestToken });
}).RequireAuthorization();

app.Run();
