using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Modules.Shipments.Domain.Enums;
using Modules.Shipments.Tests.Integration.Configuration;
using Modules.Shipments.Tests.Integration.Contracts.Requests;
using Modules.Shipments.Tests.Integration.Contracts.Responses;

namespace Modules.Shipments.Tests.Integration.Tests.GetShipmentByNumber;

[Collection("ShippingTests")]
public class GetShipmentByNumberTests(CustomWebApplicationFactory webFactory)
	: BaseTest(webFactory), IAsyncLifetime
{
	private readonly JsonSerializerOptions _jsonSerializerOptions = new()
	{
		Converters = { new JsonStringEnumConverter() },
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync() => await WebFactory.ResetDatabaseAsync();

	[Fact]
	public async Task GetShipmentByNumber_ShouldReturnShipment_WhenShipmentExists()
	{
		// Arrange
		var address = new AddressResponse("Amazing st. 5", "New York", "127675");
		List<ShipmentItemRequest> items = [ new("Samsung Electronics", 1) ];
		List<ShipmentItemResponse> responseItems = [ new("Samsung Electronics", 1) ];

		// Act
		var shipmentResponse = await CreateShipmentAsync(address, items);
		var shipmentNumber = shipmentResponse.Number;

		var httpResponse = await WebFactory.HttpClient.GetAsync($"/api/shipments/{shipmentNumber}");
		shipmentResponse = (await httpResponse.Content.ReadFromJsonAsync<ShipmentResponse>(_jsonSerializerOptions))!;

		// Assert
		Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);

		var expectedResponse = new ShipmentResponse(shipmentResponse.Number, "12345", address, "Modern Shipping", "test@mail.com",
			ShipmentStatus.Created, responseItems);

		Assert.Equivalent(expectedResponse, shipmentResponse);
	}

	[Fact]
	public async Task GetShipmentByNumber_ShouldReturnNotFound_WhenShipmentIsNotFound()
	{
		// Arrange
		// Act
		var httpResponse = await WebFactory.HttpClient.GetAsync("/api/shipments/12345");
		var validationResult = await httpResponse.Content.ReadFromJsonAsync<ValidationProblemDetails>();

		// Assert
		Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
		Assert.NotNull(validationResult);

		var error = validationResult.Errors.FirstOrDefault();
		Assert.Equal("Shipments.NotFound", error.Key);
		Assert.Equal("Shipment with number '12345' not found", error.Value.First());
	}

	private async Task<ShipmentResponse> CreateShipmentAsync(AddressResponse address, List<ShipmentItemRequest> items)
	{
		var request = new CreateShipmentRequest("12345", address, "Modern Shipping", "test@mail.com", items);

		var authToken = await LoginUserAsync();
		await CreateCarrierAsync(authToken, "Modern Shipping");
		await CreateStockAsync(authToken, "Samsung Electronics", 10);

		var httpResponse = await WebFactory.HttpClient.PostAsJsonAsync("/api/shipments", request);
		return (await httpResponse.Content.ReadFromJsonAsync<ShipmentResponse>(_jsonSerializerOptions))!;
	}
}
