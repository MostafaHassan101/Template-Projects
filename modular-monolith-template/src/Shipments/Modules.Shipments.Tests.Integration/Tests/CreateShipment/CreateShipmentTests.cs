using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Modules.Shipments.Domain.Enums;
using Modules.Shipments.Tests.Integration.Configuration;
using Modules.Shipments.Tests.Integration.Contracts.Requests;
using Modules.Shipments.Tests.Integration.Contracts.Responses;

namespace Modules.Shipments.Tests.Integration.Tests.CreateShipment;

[Collection("ShippingTests")]
public class CreateShipmentTests(CustomWebApplicationFactory webFactory)
	: BaseTest(webFactory), IAsyncLifetime
{
	public Task InitializeAsync() => Task.CompletedTask;

	public async Task DisposeAsync() => await WebFactory.ResetDatabaseAsync();

	[Fact]
	public async Task CreateShipment_ShouldSucceed_WhenRequestIsValid()
	{
		// Arrange
		var address = new AddressResponse("Amazing st. 5", "New York", "127675");

		List<ShipmentItemRequest> items = [ new("Samsung Electronics", 1) ];
		List<ShipmentItemResponse> responseItems = [ new("Samsung Electronics", 1) ];

		var request = new CreateShipmentRequest("12345", address, "Modern Shipping", "test@mail.com", items);

		// Act
		var authToken = await LoginUserAsync();
		await CreateCarrierAsync(authToken, "Modern Shipping");
		await CreateStockAsync(authToken, "Samsung Electronics", 10);

		var httpResponse = await WebFactory.HttpClient.PostAsJsonAsync("/api/shipments", request);
		var shipmentResponse = (await httpResponse.Content.ReadFromJsonAsync<ShipmentResponse>(JsonSerializerOptions))!;

		// Assert
		Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);

		var expectedResponse = new ShipmentResponse(shipmentResponse.Number, "12345", address, "Modern Shipping", "test@mail.com",
			ShipmentStatus.Created, responseItems);

		Assert.Equivalent(expectedResponse, shipmentResponse);
	}

	[Fact]
	public async Task CreateShipment_ShouldFail_WhenProductsAreMissingInStock()
	{
		// Arrange
		var address = new AddressResponse("Amazing st. 5", "New York", "127675");

		List<ShipmentItemRequest> items = [ new("Samsung Electronics", 10) ];

		var request = new CreateShipmentRequest("12345", address, "Modern Shipping", "test@mail.com", items);

		// Act
		var httpResponse = await WebFactory.HttpClient.PostAsJsonAsync("/api/shipments", request);
		var validationResult = await httpResponse.Content.ReadFromJsonAsync<ValidationProblemDetails>();

		// Assert
		Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
		Assert.NotNull(validationResult);
		Assert.Single(validationResult.Errors);

		var error = validationResult.Errors.FirstOrDefault();
		Assert.Equal("Stocks.ProductNotFound", error.Key);
		Assert.Equal("Product Samsung Electronics not found in stock", error.Value.First());
	}

	[Fact]
	public async Task CreateShipment_ShouldFail_WhenNotEnoughProductsInStock()
	{
		// Arrange
		var address = new AddressResponse("Amazing st. 5", "New York", "127675");

		List<ShipmentItemRequest> items = [ new("Samsung Electronics", 10) ];

		var request = new CreateShipmentRequest("12345", address, "Modern Shipping", "test@mail.com", items);

		// Act
		var authToken = await LoginUserAsync();
		await CreateStockAsync(authToken, "Samsung Electronics", 5);

		var httpResponse = await WebFactory.HttpClient.PostAsJsonAsync("/api/shipments", request);
		var validationResult = await httpResponse.Content.ReadFromJsonAsync<ValidationProblemDetails>();

		// Assert
		Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
		Assert.NotNull(validationResult);
		Assert.Single(validationResult.Errors);

		var error = validationResult.Errors.FirstOrDefault();
		Assert.Equal("Stocks.InsufficientStocks", error.Key);
		Assert.Equal("Insufficient stock for product Samsung Electronics. Required: 10, Available: 5", error.Value.First());
	}

	[Fact]
	public async Task CreateShipment_ShouldFail_WhenZeroProductsSentInRequest()
	{
		// Arrange
		var address = new AddressResponse("Amazing st. 5", "New York", "127675");

		List<ShipmentItemRequest> items = [ new("Samsung Electronics", 0) ];

		var request = new CreateShipmentRequest("12345", address, "Modern Shipping", "test@mail.com", items);

		// Act
		var httpResponse = await WebFactory.HttpClient.PostAsJsonAsync("/api/shipments", request);
		var validationResult = await httpResponse.Content.ReadFromJsonAsync<ValidationProblemDetails>();

		// Assert
		Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
		Assert.Equal(1, validationResult?.Errors.Count);
	}

	[Fact]
	public async Task CreateShipment_ShouldReturnConflict_WhenShipmentForOrderIsAlreadyCreated()
	{
		// Arrange
		var address = new AddressResponse("Amazing st. 5", "New York", "127675");

		List<ShipmentItemRequest> items = [ new("Samsung Electronics", 1) ];

		var request = new CreateShipmentRequest("12345", address, "Modern Shipping", "test@mail.com", items);

		// Act
		var authToken = await LoginUserAsync();
		await CreateCarrierAsync(authToken, "Modern Shipping");
		await CreateStockAsync(authToken, "Samsung Electronics", 10);

		// Create first shipment
		await WebFactory.HttpClient.PostAsJsonAsync("/api/shipments", request);

		// Create duplicate shipment
		var httpResponse = await WebFactory.HttpClient.PostAsJsonAsync("/api/shipments", request);
		var validationResult = await httpResponse.Content.ReadFromJsonAsync<ValidationProblemDetails>();

		// Assert
		Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
		Assert.NotNull(validationResult);
		Assert.Single(validationResult.Errors);

		var error = validationResult.Errors.FirstOrDefault();
		Assert.Equal("Shipments.AlreadyExists", error.Key);
		Assert.Equal("Shipment for order '12345' already exists", error.Value.First());
	}

	[Fact]
	public async Task CreateShipment_ShouldReturnBadRequest_WhenRequestHasNoItems()
	{
		// Arrange
		var address = new AddressResponse("Amazing st. 5", "New York", "127675");

		var request = new CreateShipmentRequest("12345", address, "Modern Shipping", "test@mail.com", []);

		// Act
		await WebFactory.HttpClient.PostAsJsonAsync("/api/shipments", request);
		var httpResponse = await WebFactory.HttpClient.PostAsJsonAsync("/api/shipments", request);
		var validationResult = await httpResponse.Content.ReadFromJsonAsync<ValidationProblemDetails>();

		// Assert
		Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
		Assert.NotNull(validationResult);
		Assert.Single(validationResult.Errors);

		var error = validationResult.Errors.FirstOrDefault();
		Assert.Equal("Items", error.Key);
		Assert.Equal("'Items' must not be empty.", error.Value.First());
	}
}
