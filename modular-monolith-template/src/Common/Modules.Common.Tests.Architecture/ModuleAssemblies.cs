using System.Reflection;

namespace Modules.Common.Tests.Architecture;

internal static class ModuleAssemblies
{
    // Users module assemblies
    internal static readonly Assembly UsersDomainAssembly = Users.Domain.AssemblyReference.Assembly;
    internal static readonly Assembly UsersFeaturesAssembly = Users.Features.AssemblyReference.Assembly;
    internal static readonly Assembly UsersInfrastructureAssembly = Users.Infrastructure.AssemblyReference.Assembly;

    // Shipments module assemblies
    internal static readonly Assembly ShipmentsDomainAssembly = Shipments.Domain.AssemblyReference.Assembly;
    internal static readonly Assembly ShipmentsFeaturesAssembly = Shipments.Features.AssemblyReference.Assembly;
    internal static readonly Assembly ShipmentsInfrastructureAssembly = Shipments.Infrastructure.AssemblyReference.Assembly;

    // Stocks module assemblies
    internal static readonly Assembly StocksDomainAssembly = Stocks.Domain.AssemblyReference.Assembly;
    internal static readonly Assembly StocksFeaturesAssembly = Stocks.Features.AssemblyReference.Assembly;
    internal static readonly Assembly StocksInfrastructureAssembly = Stocks.Infrastructure.AssemblyReference.Assembly;
    internal static readonly Assembly StocksPublicApiAssembly = Stocks.PublicApi.AssemblyReference.Assembly;

    // Carriers module assemblies
    internal static readonly Assembly CarriersDomainAssembly = Carriers.Domain.AssemblyReference.Assembly;
    internal static readonly Assembly CarriersFeaturesAssembly = Carriers.Features.AssemblyReference.Assembly;
    internal static readonly Assembly CarriersInfrastructureAssembly = Carriers.Infrastructure.AssemblyReference.Assembly;
    internal static readonly Assembly CarriersPublicApiAssembly = Carriers.PublicApi.AssemblyReference.Assembly;
}
