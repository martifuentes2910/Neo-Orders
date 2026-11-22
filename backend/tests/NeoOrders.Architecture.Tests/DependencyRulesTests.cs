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

    [Fact]
    public void Domain_ProjectFile_ShouldNotReferenceEfOrAspNetOrIdentity()
    {
        var root = FindRepoRoot();
        var csproj = File.ReadAllText(Path.Combine(root, "backend", "src", "NeoOrders.Domain", "NeoOrders.Domain.csproj"));
        Assert.DoesNotContain("EntityFramework", csproj);
        Assert.DoesNotContain("AspNetCore", csproj);
        Assert.DoesNotContain("Identity", csproj);
        Assert.DoesNotContain("PackageReference", csproj);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "NeoOrdersApp.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("Repo root not found");
    }
}
