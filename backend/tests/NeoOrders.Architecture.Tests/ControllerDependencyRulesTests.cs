using NetArchTest.Rules;

namespace NeoOrders.Architecture.Tests;

public class ControllerDependencyRulesTests
{
    [Fact]
    public void Controllers_ShouldNotDependOn_AppDbContext()
    {
        var result = Types.InAssembly(typeof(NeoOrders.Api.Controllers.OrdersController).Assembly)
            .That().ResideInNamespace("NeoOrders.Api.Controllers")
            .ShouldNot().HaveDependencyOn("NeoOrders.Infrastructure.Data.AppDbContext")
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }
}
