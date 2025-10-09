using System.Diagnostics;

namespace Modules.Carriers.Features.Tracing;

internal static class CarriersActivitySource
{
    internal static readonly ActivitySource Instance = new("carriers");
}
