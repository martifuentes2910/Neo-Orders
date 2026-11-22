namespace NeoOrders.Application.Tests;

public class SmokeTests
{
    [Fact]
    public void ApplicationAssembly_Loads()
    {
        Assert.Equal("NeoOrders.Application", typeof(ApplicationAssemblyMarker).Assembly.GetName().Name);
    }
}
