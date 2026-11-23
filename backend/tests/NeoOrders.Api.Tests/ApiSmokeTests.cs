using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeoOrders.Infrastructure.Data;
using NeoOrders.Infrastructure.Models;

namespace NeoOrders.Api.Tests;

public class ApiSmokeTests : IClassFixture<NeoOrdersApiFactory>
{
    private readonly NeoOrdersApiFactory _factory;

    public ApiSmokeTests(NeoOrdersApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetVentasVendedor_PageInvalida_Retorna400ConContratoMessage()
    {
        using var auth = _factory.CreateClientWithTestAuth();

        var response = await auth.Client.GetAsync("/api/reports/ventas-vendedor?page=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await ReadJsonAsync(response);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("message", out var message));
        Assert.Contains("page", message.GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetNotifications_SinAuth_Retorna401SinBody()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/notifications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(string.IsNullOrWhiteSpace(body));
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }
}

public sealed class NeoOrdersApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seed:RunOnStartup"] = "false",
                ["Cloudinary:CloudName"] = "",
                ["Cloudinary:ApiKey"] = "",
                ["Cloudinary:ApiSecret"] = ""
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Registrar opciones "limpias": el AddDbContext de Program.cs deja
            // configuraciones IConfigureOptions con UseSqlServer que, de aplicarse,
            // mezclan providers y rompen cualquier acceso a Identity.
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(AppDbContext));

            var dbName = $"NeoOrdersApiTests-{Guid.NewGuid()}";
            services.AddScoped<DbContextOptions<AppDbContext>>(_ =>
                new DbContextOptionsBuilder<AppDbContext>()
                    .UseInMemoryDatabase(dbName)
                    .Options);
            services.AddScoped(sp =>
                new AppDbContext(sp.GetRequiredService<DbContextOptions<AppDbContext>>()));
        });
    }

        /// <summary>
    /// Cliente con esquema de autenticaci?n de prueba. Sin argumentos mantiene el
    /// comportamiento original (claims "test-user", sin roles).
    /// </summary>
    public TestAuthClient CreateClientWithTestAuth(
        string? userId = null,
        string? name = null,
        IEnumerable<string>? roles = null,
        Action<IServiceCollection>? configureServices = null) =>
        CreateTestClient(disableAntiforgery: true, userId, name, roles, configureServices);

    /// <summary>
    /// Cliente con auth de prueba y validaci?n antiforgery real habilitada.
    /// Usar junto con <see cref="WithAntiforgeryTokenAsync"/> para cubrir XSRF.
    /// </summary>
    public TestAuthClient CreateClientWithTestAuthAndAntiforgery(
        string? userId = null,
        string? name = null,
        IEnumerable<string>? roles = null) =>
        CreateTestClient(disableAntiforgery: false, userId, name, roles, configureServices: null);

    private TestAuthClient CreateTestClient(
        bool disableAntiforgery,
        string? userId,
        string? name,
        IEnumerable<string>? roles,
        Action<IServiceCollection>? configureServices)
    {
        var factory = WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                        options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                        options.DefaultScheme = TestAuthHandler.SchemeName;
                    })
                    .AddScheme<TestAuthOptions, TestAuthHandler>(TestAuthHandler.SchemeName, options =>
                    {
                        options.UserId = userId ?? TestAuthOptions.DefaultUserId;
                        options.Name = name ?? TestAuthOptions.DefaultName;
                        options.Roles = roles?.ToArray() ?? [];
                    });
                services.AddAuthorization();

                // TestServer corre sobre HTTP; en Testing la cookie antiforgery es Always.
                // Los clientes con auth por esquema Test (header) no usan cookie auth y
                // AutoValidateAntiforgeryToken no aplica a auth no-cookie; se relaja la
                // política de cookie solo en esta factory derivada de tests.
                services.PostConfigure<AntiforgeryOptions>(o => o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest);
                services.PostConfigure<MvcOptions>(o => o.Filters.Add(new IgnoreAntiforgeryTokenAttribute()));

                // Gancho para tests de contrato: permite sustituir un servicio con un stub
                // (p. ej. simular un UNIQUE o una excepción no manejada) sin tocar el producto.
                configureServices?.Invoke(services);
            });
        });

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName);
        return new TestAuthClient(factory, client);
    }
}

public sealed class TestAuthClient : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public TestAuthClient(WebApplicationFactory<Program> factory, HttpClient client)
    {
        _factory = factory;
        Client = client;
    }

    public HttpClient Client { get; }

    /// <summary>
    /// Siembra un usuario (y opcionalmente roles) directamente en el DB InMemory
    /// de este factory derivado, sin depender de login real.
    /// </summary>
    public async Task SeedUserAsync(ApplicationUser user, params string[] roles)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var created = await userManager.CreateAsync(user);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException(
                $"No se pudo sembrar el usuario de prueba: {string.Join(" ", created.Errors.Select(e => e.Description))}");
        }

        foreach (var role in roles.Distinct())
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"No se pudo crear el rol de prueba '{role}': {string.Join(" ", roleResult.Errors.Select(e => e.Description))}");
                }
            }

            await userManager.AddToRoleAsync(user, role);
        }
    }

    /// <summary>
    /// Ejecuta una consulta (o siembra) sobre el AppDbContext InMemory de este
    /// factory derivado. CreateClientWithTestAuth crea una factory propia con su
    /// propia DB aislada: usar su scope garantiza que los datos sembrados sean
    /// los mismos que ve el HttpClient de este cliente.
    /// </summary>
    public async Task<T> QueryDbAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await query(db);
    }

    public void Dispose()
    {
        Client.Dispose();
        _factory.Dispose();
    }
}

internal sealed class TestAuthOptions : AuthenticationSchemeOptions
{
    public const string DefaultUserId = "test-user";
    public const string DefaultName = "test-user";

    public string UserId { get; set; } = DefaultUserId;
    public string Name { get; set; } = DefaultName;
    public string[] Roles { get; set; } = [];
}

internal sealed class TestAuthHandler : AuthenticationHandler<TestAuthOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(
        IOptionsMonitor<TestAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Options.UserId),
            new(ClaimTypes.Name, Options.Name)
        };
        claims.AddRange(Options.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
