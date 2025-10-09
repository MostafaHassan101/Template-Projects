using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Modules.Shipments.Tests.Integration.Configuration;
using Modules.Shipments.Tests.Integration.Contracts.Requests;
using Modules.Shipments.Tests.Integration.Contracts.Responses;

namespace Modules.Shipments.Tests.Integration.Tests;

public abstract class BaseTest(CustomWebApplicationFactory webFactory)
{
	protected CustomWebApplicationFactory WebFactory => webFactory;

	protected JsonSerializerOptions JsonSerializerOptions => new()
	{
		Converters = { new JsonStringEnumConverter() },
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	protected async Task<string> LoginUserAsync()
	{
		var request = new LoginUserRequest("admin@test.com", "Test1234!");

		var httpResponse = await WebFactory.HttpClient.PostAsJsonAsync("/api/users/login", request);
		var response = await httpResponse.Content.ReadFromJsonAsync<LoginUserResponse>(JsonSerializerOptions);
		return response!.Token;
	}

	protected async Task<ShipmentResponse> CreateShipmentAsync(CreateShipmentRequest request)
	{
		var httpResponse = await WebFactory.HttpClient.PostAsJsonAsync("/api/shipments", request);
		var response = await httpResponse.Content.ReadFromJsonAsync<ShipmentResponse>(JsonSerializerOptions);

		return response!;
	}

	protected async Task CreateStockAsync(string authToken, string productName, int quantity)
	{
		var request = new CreateStockRequest(productName, quantity);

		WebFactory.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

		var httpResponse = await WebFactory.HttpClient.PostAsJsonAsync("/api/stocks", request);
		await httpResponse.Content.ReadFromJsonAsync<CreateStockResponse>(JsonSerializerOptions);
	}

	protected async Task CreateCarrierAsync(string authToken, string carrierName)
	{
		var request = new CreateCarrierRequest(carrierName);

		WebFactory.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

		var httpResponse = await WebFactory.HttpClient.PostAsJsonAsync("/api/carriers", request);
		await httpResponse.Content.ReadFromJsonAsync<CreateCarrierResponse>(JsonSerializerOptions);
	}
}
