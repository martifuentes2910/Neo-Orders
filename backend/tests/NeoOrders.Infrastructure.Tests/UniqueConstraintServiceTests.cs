using Microsoft.EntityFrameworkCore;
using NeoOrders.Infrastructure.Data;
using NeoOrders.Infrastructure.Services;

namespace NeoOrders.Infrastructure.Tests;

public class UniqueConstraintServiceTests
{
    private sealed class ThrowUniqueDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        public bool ThrowUniqueOnSave { get; set; }
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (ThrowUniqueOnSave)
                throw new DbUpdateException("unique", new Exception("UNIQUE constraint failed: Clients.Email"));
            return base.SaveChangesAsync(cancellationToken);
        }
    }

    private static ThrowUniqueDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ThrowUniqueDbContext(options);
    }

    [Fact]
    public async Task ClientService_CreateAsync_MapsUniqueConstraintToInvalidOperation()
    {
        using var db = CreateDb();
        var svc = new ClientService(db);
        db.ThrowUniqueOnSave = true;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(new NeoOrders.Application.DTOs.Clients.ClientDto { Nombre = "A", Email = "a@a.com" }));

        Assert.Contains("email", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
