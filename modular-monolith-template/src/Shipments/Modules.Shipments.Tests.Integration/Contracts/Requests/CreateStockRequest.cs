namespace Modules.Shipments.Tests.Integration.Contracts.Requests;

public sealed record CreateStockRequest(string ProductName, int Quantity);
