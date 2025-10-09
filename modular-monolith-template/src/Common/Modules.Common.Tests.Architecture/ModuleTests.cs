using System.Reflection;
using NetArchTest.Rules;

namespace Modules.Common.Tests.Architecture;

public class ModuleTests
{
	private const string UsersNamespace = "Modules.Users";
	private const string ShipmentsNamespace = "Modules.Shipments";
	private const string CarriersNamespace = "Modules.Carriers";
	private const string StocksNamespace = "Modules.Stocks";

	// Project namespaces
	private const string CarriersDomainNamespace = CarriersNamespace + ".Domain";
	private const string CarriersInfraNamespace = CarriersNamespace + ".Infrastructure";
	private const string CarriersFeaturesNamespace = CarriersNamespace + ".Features";
	private const string CarriersPublicApiNamespace = CarriersNamespace + ".PublicApi";

	private const string StocksDomainNamespace = StocksNamespace + ".Domain";
	private const string StocksInfraNamespace = StocksNamespace + ".Infrastructure";
	private const string StocksFeaturesNamespace = StocksNamespace + ".Features";
	private const string StocksPublicApiNamespace = StocksNamespace + ".PublicApi";

    [Fact]
    public void UsersModule_ShouldNotHaveDependencyOn_AnyOtherModule()
    {
        var result = Types.InAssemblies(GetUsersModuleAssemblies())
            .Should()
            .NotHaveDependencyOnAny(
	            CarriersNamespace,
	            StocksNamespace,
                ShipmentsNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void CarriersModule_ShouldNotHaveDependencyOn_AnyOtherModule()
    {
        var result = Types.InAssemblies(GetCarriersModuleAssemblies())
            .Should()
            .NotHaveDependencyOnAny(
	            UsersNamespace,
	            StocksNamespace,
	            ShipmentsNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void StocksModule_ShouldNotHaveDependencyOn_AnyOtherModule()
    {
        var result = Types.InAssemblies(GetStocksModuleAssemblies())
            .Should()
            .NotHaveDependencyOnAny(
	            UsersNamespace,
                CarriersNamespace,
                ShipmentsNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void ShipmentsModule_ShouldOnlyDependOn_CarriersAndStocksModules()
    {
        var result = Types.InAssemblies(GetShipmentsModuleAssemblies())
            .Should()
            .NotHaveDependencyOnAny(UsersNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void ShipmentsModule_ShouldOnlyReference_PublicApiProjects()
    {
        var shipmentsAssemblies = GetShipmentsModuleAssemblies();

        // Check that Shipments module doesn't reference internal Carriers projects
        var carriersInternalResult = Types.InAssemblies(shipmentsAssemblies)
            .Should()
            .NotHaveDependencyOnAny(
                CarriersDomainNamespace,
                CarriersInfraNamespace,
                CarriersFeaturesNamespace)
            .GetResult();

        Assert.True(carriersInternalResult.IsSuccessful,
            $"Shipments module should not reference internal Carriers projects: {string.Join(", ", carriersInternalResult.FailingTypeNames ?? [])}");

        // Check that Shipments module doesn't reference internal Stocks projects
        var stocksInternalResult = Types.InAssemblies(shipmentsAssemblies)
            .Should()
            .NotHaveDependencyOnAny(
                StocksDomainNamespace,
                StocksInfraNamespace,
                StocksFeaturesNamespace)
            .GetResult();

        Assert.True(stocksInternalResult.IsSuccessful,
            $"Shipments module should not reference internal Stocks projects: {string.Join(", ", stocksInternalResult.FailingTypeNames ?? [])}");

        // Verify that it DOES reference the PublicApi projects (optional positive test)
        var hasCarriersPublicApiDep = Types.InAssemblies(shipmentsAssemblies)
            .That()
            .HaveDependencyOn(CarriersPublicApiNamespace)
            .GetTypes()
            .Any();

        var hasStocksPublicApiDep = Types.InAssemblies(shipmentsAssemblies)
            .That()
            .HaveDependencyOn(StocksPublicApiNamespace)
            .GetTypes()
            .Any();

        Assert.True(hasCarriersPublicApiDep || hasStocksPublicApiDep,
            "Shipments module should reference at least one PublicApi project");
    }

    private static Assembly[] GetUsersModuleAssemblies()
    {
        return [
            ModuleAssemblies.UsersDomainAssembly,
            ModuleAssemblies.UsersInfrastructureAssembly,
            ModuleAssemblies.UsersFeaturesAssembly
        ];
    }

    private static Assembly[] GetCarriersModuleAssemblies()
    {
        return [
            ModuleAssemblies.CarriersDomainAssembly,
            ModuleAssemblies.CarriersInfrastructureAssembly,
            ModuleAssemblies.CarriersFeaturesAssembly,
            ModuleAssemblies.CarriersPublicApiAssembly
        ];
    }

    private static Assembly[] GetStocksModuleAssemblies()
    {
        return [
            ModuleAssemblies.StocksDomainAssembly,
            ModuleAssemblies.StocksInfrastructureAssembly,
            ModuleAssemblies.StocksFeaturesAssembly,
            ModuleAssemblies.StocksPublicApiAssembly
        ];
    }

    private static Assembly[] GetShipmentsModuleAssemblies()
    {
        return [
            ModuleAssemblies.ShipmentsDomainAssembly,
            ModuleAssemblies.ShipmentsInfrastructureAssembly,
            ModuleAssemblies.ShipmentsFeaturesAssembly
        ];
    }
}
