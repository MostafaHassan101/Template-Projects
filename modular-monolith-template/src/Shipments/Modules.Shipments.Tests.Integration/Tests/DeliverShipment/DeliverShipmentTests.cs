using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Modules.Shipments.Tests.Integration.Configuration;
using Modules.Shipments.Tests.Integration.Contracts.Requests;
using Modules.Shipments.Tests.Integration.Contracts.Responses;

namespace Modules.Shipments.Tests.Integration.Tests.DeliverShipment;

[Collection("ShippingTests")]
public class DeliverShipmentTests(CustomWebApplicationFactory webFactory)
	: BaseTest(webFactory), IAsyncLifetime
{
	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync() => await WebFactory.ResetDatabaseAsync();

	[Fact]
	public async Task DeliverShipment_ShouldDeliverShipment_WhenShipmentExists()
	{
		// Arrange
		var address = new AddressResponse("Amazing st. 5", "New York", "127675");

		List<ShipmentItemRequest> items = [ new("Samsung Electronics", 1) ];

		var request = new CreateShipmentRequest("12345", address, "Modern Shipping", "test@mail.com", items);

		// Act
		var authToken = await LoginUserAsync();
		await CreateCarrierAsync(authToken, "Modern Shipping");
		await CreateStockAsync(authToken, "Samsung Electronics", 10);
		var shipment = await CreateShipmentAsync(request);

		await WebFactory.HttpClient.PostAsync($"/api/shipments/process/{shipment.Number}", null);
		await WebFactory.HttpClient.PostAsync($"/api/shipments/dispatch/{shipment.Number}", null);
		await WebFactory.HttpClient.PostAsync($"/api/shipments/transit/{shipment.Number}", null);
		var httpResponse = await WebFactory.HttpClient.PostAsync($"/api/shipments/deliver/{shipment.Number}", null);

		// Assert
		Assert.Equal(HttpStatusCode.NoContent, httpResponse.StatusCode);
	}

	[Fact]
	public async Task DeliverShipment_ShouldReturnError_WhenShipmentDoesNotExist()
	{
		// Arrange
		// Act
		var httpResponse = await WebFactory.HttpClient.PostAsync("/api/shipments/deliver/12345", null);
		var validationResult = await httpResponse.Content.ReadFromJsonAsync<ValidationProblemDetails>();

		// Assert
		Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
		Assert.NotNull(validationResult);
		Assert.Single(validationResult.Errors);

		var error = validationResult.Errors.FirstOrDefault();
		Assert.Equal("Shipment.NotFound", error.Key);
		Assert.Equal("Shipment with number '12345' not found", error.Value.First());
	}

	[Fact]
	public async Task DeliverShipment_ShouldReturnError_WhenShipmentCannotBeDelivered()
	{
		// Arrange
		var address = new AddressResponse("Amazing st. 5", "New York", "127675");

		List<ShipmentItemRequest> items = [ new("Samsung Electronics", 10) ];

		var request = new CreateShipmentRequest("12345", address, "Modern Shipping", "test@mail.com", items);

		// Act
		var authToken = await LoginUserAsync();
		await CreateCarrierAsync(authToken, "Modern Shipping");
		await CreateStockAsync(authToken, "Samsung Electronics", 10);
		var shipment = await CreateShipmentAsync(request);

		var httpResponse = await WebFactory.HttpClient.PostAsync($"/api/shipments/deliver/{shipment.Number}", null);
		var validationResult = await httpResponse.Content.ReadFromJsonAsync<ValidationProblemDetails>();

		// Assert
		Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
		Assert.NotNull(validationResult);
		Assert.Single(validationResult.Errors);

		var error = validationResult.Errors.FirstOrDefault();
		Assert.Equal("Shipments.Validation", error.Key);
		Assert.Equal($"Can only update to Delivered from InTransit status for shipment {shipment.Number}", error.Value.First());
	}
}
