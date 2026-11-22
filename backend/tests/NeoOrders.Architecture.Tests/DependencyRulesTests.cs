using NetArchTest.Rules;
using NeoOrders.Application;
using NeoOrders.Domain;
using NeoOrders.Infrastructure;

namespace NeoOrders.Architecture.Tests;

public class DependencyRulesTests
{
    [Fact]
    public void Domain_ShouldNotDependOn_OtherLayers()
    {
        var result = Types.InAssembly(typeof(DomainAssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("NeoOrders.Application", "NeoOrders.Infrastructure", "NeoOrders.Api")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Application_ShouldOnlyDependOn_Domain()
    {
        var result = Types.InAssembly(typeof(ApplicationAssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("NeoOrders.Infrastructure", "NeoOrders.Api")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOn_Api()
    {
        var result = Types.InAssembly(typeof(InfrastructureAssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("NeoOrders.Api")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}
