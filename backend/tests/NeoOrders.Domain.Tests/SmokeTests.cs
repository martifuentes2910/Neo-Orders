namespace NeoOrders.Domain.Tests;

public class SmokeTests
{
    [Fact]
    public void DomainAssembly_Loads()
    {
        Assert.Equal("NeoOrders.Domain", typeof(DomainAssemblyMarker).Assembly.GetName().Name);
    }
}
