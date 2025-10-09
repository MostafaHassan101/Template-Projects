namespace Modules.Stocks.Features.Features.Shared.Routes;

internal static class RouteConsts
{
    internal const string BaseRoute = "/api/stocks";
    
    internal const string Create = BaseRoute;
    internal const string IncreaseStock = $"{BaseRoute}/increase";
    internal const string GetStocksByProductName = $"{BaseRoute}/{{productName}}";
}