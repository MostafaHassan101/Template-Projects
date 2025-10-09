using System.Diagnostics;

namespace Modules.Stocks.Features.Tracing;

internal static class StocksActivitySource
{
    internal static readonly ActivitySource Instance = new("stocks");
}
