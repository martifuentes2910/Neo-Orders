using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NeoOrders.Infrastructure.Models;

namespace NeoOrders.Api.Tests;

/// <summary>
/// Flujo REAL de cookie de autenticación (Identity) + antiforgery (XSRF), aislado
/// del harness de header-auth usado por el resto de los contratos:
/// <c>TestAuthHandler</c> no es un esquema de cookie y el factory derivado de
/// <see cref="NeoOrdersApiFactory.CreateClientWithTestAuth"/> relaja la validación
/// con IgnoreAntiforgeryToken. Aquí no se registra ese esquema ni ese filtro,
/// por lo que AutoValidateAntiforgeryToken queda activo.
///
/// TestServer corre sobre HTTP y en Testing las cookies se configuran como
/// SecurePolicy.Always / SameSite.None; solo en ESTE factory derivado se ajustan
/// a SameAsRequest / Lax para que HttpClient las devuelva en el mismo cliente.
/// No se modifica Program.cs ni la configuración de producción.
/// </summary>
public class CookieAntiforgeryContractTests : IClassFixture<NeoOrdersApiFactory>
{
    private readonly NeoOrdersApiFactory _factory;

    public CookieAntiforgeryContractTests(NeoOrdersApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAntiforgeryToken_SinAuth_Retorna401()
    {
        using var factory = CreateCookieFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/antiforgery/token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutConfigProfile_ConCookieAuthYTipoXsrf_Retorna200YPersiste()
    {
        using var factory = CreateCookieFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var email = await SeedAndLoginAsync(factory, client);

        var token = await GetXsrfTokenAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/config/profile")
        {
            Content = JsonContent.Create(new
            {
                nombre = "CookieActualizado",
                apellido = "",
                avatar = "",
                tema = "",
                paleta = ""
            })
        };
        request.Headers.Add("X-XSRF-TOKEN", token);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            Assert.NotNull(user);
            Assert.Equal("CookieActualizado", user.Nombre);
        }
    }

    [Fact]
    public async Task PutConfigProfile_ConCookieAuthSinTokenXsrf_Retorna400()
    {
        using var factory = CreateCookieFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await SeedAndLoginAsync(factory, client);

        // Se obtiene el token (y su cookie) igualmente: sin enviar el header,
        // AutoValidateAntiforgeryToken debe rechazar el unsafe request.
        _ = await GetXsrfTokenAsync(client);

        var response = await client.PutAsJsonAsync("/api/config/profile", new { nombre = "SinToken" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private WebApplicationFactory<Program> CreateCookieFactory() =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // TestServer sobre HTTP: sin este ajuste HttpClient no devuelve
                // las cookies Secure=None y el flujo no puede completarse.
                // PostConfigureAll (no PostConfigure): la cookie de Identity se
                // registra bajo el nombre "Identity.Application".
                services.PostConfigureAll<AntiforgeryOptions>(o =>
                {
                    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    o.Cookie.SameSite = SameSiteMode.Lax;
                });
                services.PostConfigureAll<CookieAuthenticationOptions>(o =>
                {
                    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    o.Cookie.SameSite = SameSiteMode.Lax;
                });
            });
        });

    /// <summary>Siembra un usuario con password y hace login real (cookie de Identity).</summary>
    private static async Task<string> SeedAndLoginAsync(
        WebApplicationFactory<Program> factory, HttpClient client)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"cookie.xsrf.{suffix}@test.local";
        const string password = "Clave123!";

        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var created = await userManager.CreateAsync(new ApplicationUser
            {
                Id = $"cookie-xsrf-{suffix}",
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Nombre = "Inicial",
                Apellido = "Usuario"
            }, password);
            Assert.True(created.Succeeded, string.Join(" ", created.Errors.Select(e => e.Description)));
        }

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return email;
    }

    private static async Task<string> GetXsrfTokenAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/antiforgery/token");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies, c => c.Contains("NeoOrders.XSRF", StringComparison.OrdinalIgnoreCase));

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var token = GetProperty(doc.RootElement, "token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token!;
    }

    private static JsonElement GetProperty(JsonElement obj, string name)
    {
        if (obj.TryGetProperty(name, out var exact)) return exact;
        foreach (var property in obj.EnumerateObject())
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                return property.Value;
        throw new Xunit.Sdk.XunitException($"La respuesta JSON no contiene la propiedad '{name}'.");
    }
}
