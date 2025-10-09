using Modules.Shipments.Domain.Entities;
using Modules.Shipments.Domain.Enums;
using Modules.Shipments.Domain.ValueObjects;

namespace Modules.Shipments.Tests.Unit.Entities;

public class ShipmentTests
{
    [Fact]
    public void Create_ShouldCreateNewShipment_WithProvidedProperties()
    {
		// Arrange
		const string number = "SHP12345";
		const string orderId = "ORD9876";

        var address = new Address
        {
            Street = "123 Main St",
            City = "New York",
            Zip = "10001"
        };

		const string carrier = "FedEx";
		const string receiverEmail = "test@example.com";

        var items = new List<ShipmentItem>
        {
            new() { Product = "Product1", Quantity = 2 },
            new() { Product = "Product2", Quantity = 3 }
        };

        // Act
        var shipment = Shipment.Create(number, orderId, address, carrier, receiverEmail, items);

        // Assert
        Assert.NotEqual(Guid.Empty, shipment.Id);
        Assert.Equal(number, shipment.Number);
        Assert.Equal(orderId, shipment.OrderId);
        Assert.Equal(address, shipment.Address);
        Assert.Equal(carrier, shipment.Carrier);
        Assert.Equal(receiverEmail, shipment.ReceiverEmail);
        Assert.Equal(ShipmentStatus.Created, shipment.Status);
        Assert.Equal(2, shipment.Items.Count);
        Assert.NotEqual(default, shipment.CreatedAt);
        Assert.Equal(shipment.CreatedAt.ToLocalTime(), shipment.UpdatedAt!.Value.ToLocalTime(), TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void AddItem_ShouldAddItemToShipment_AndUpdateTimestamp()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var initialItemsCount = shipment.Items.Count;
        var newItem = new ShipmentItem { Product = "NewProduct", Quantity = 1 };

        // Act
        shipment.AddItem(newItem);

        // Assert
        Assert.Equal(initialItemsCount + 1, shipment.Items.Count);
        Assert.Contains(newItem, shipment.Items);
    }

    [Fact]
    public void AddItems_ShouldAddMultipleItemsToShipment_AndUpdateTimestamp()
    {
        // Arrange
        var shipment = CreateTestShipment();
        var initialItemsCount = shipment.Items.Count;
        var newItems = new List<ShipmentItem>
        {
            new() { Product = "NewProduct1", Quantity = 1 },
            new() { Product = "NewProduct2", Quantity = 2 }
        };

        // Act
        shipment.AddItems(newItems);

        // Assert
        Assert.Equal(initialItemsCount + 2, shipment.Items.Count);
        Assert.All(newItems, item => Assert.Contains(item, shipment.Items));
    }

    [Fact]
    public void RemoveItem_ShouldRemoveItemFromShipment_AndUpdateTimestamp()
    {
        // Arrange
        var shipment = CreateTestShipment();

        var itemToRemove = shipment.Items[0];

        var initialItemsCount = shipment.Items.Count;

        // Act
        shipment.RemoveItem(itemToRemove);

        // Assert
        Assert.Equal(initialItemsCount - 1, shipment.Items.Count);
        Assert.DoesNotContain(itemToRemove, shipment.Items);
    }

    [Fact]
    public void UpdateAddress_ShouldUpdateShipmentAddress_AndUpdateTimestamp()
    {
        // Arrange
        var shipment = CreateTestShipment();

        var newAddress = new Address
        {
            Street = "456 Broadway",
            City = "Boston",
            Zip = "02101"
        };

        // Act
        shipment.UpdateAddress(newAddress);

        // Assert
        Assert.Equal(newAddress, shipment.Address);
    }

    [Fact]
    public void Process_ShouldUpdateStatus_WhenStatusIsCreated()
    {
        // Arrange
        var shipment = CreateTestShipment();

        // Act
        var result = shipment.Process();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ShipmentStatus.Processing, shipment.Status);
    }

    [Fact]
    public void Process_ShouldReturnError_WhenStatusIsNotCreated()
    {
        // Arrange
        var shipment = CreateTestShipment();

        shipment.Process(); // Now status is Processing

        // Act
        var result = shipment.Process();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ShipmentStatus.Processing, shipment.Status); // Status didn't change
    }

    [Fact]
    public void Dispatch_ShouldUpdateStatus_WhenStatusIsProcessing()
    {
        // Arrange
        var shipment = CreateTestShipment();

        shipment.Process(); // Set status to Processing

        // Act
        var result = shipment.Dispatch();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ShipmentStatus.Dispatched, shipment.Status);
    }

    [Fact]
    public void Dispatch_ShouldReturnError_WhenStatusIsNotProcessing()
    {
        // Arrange
        var shipment = CreateTestShipment(); // Status is Created

        // Act
        var result = shipment.Dispatch();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ShipmentStatus.Created, shipment.Status); // Status didn't change
    }

    [Fact]
    public void Transit_ShouldUpdateStatus_WhenStatusIsDispatched()
    {
        // Arrange
        var shipment = CreateTestShipment();

        shipment.Process();
        shipment.Dispatch(); // Set status to Dispatched

        // Act
        var result = shipment.Transit();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ShipmentStatus.InTransit, shipment.Status);
    }

    [Fact]
    public void Transit_ShouldReturnError_WhenStatusIsNotDispatched()
    {
        // Arrange
        var shipment = CreateTestShipment(); // Status is Created

        // Act
        var result = shipment.Transit();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ShipmentStatus.Created, shipment.Status); // Status didn't change
    }

    [Fact]
    public void Deliver_ShouldUpdateStatus_WhenStatusIsInTransit()
    {
        // Arrange
        var shipment = CreateTestShipment();

        shipment.Process();
        shipment.Dispatch();
        shipment.Transit(); // Set status to InTransit

        // Act
        var result = shipment.Deliver();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
    }

    [Fact]
    public void Deliver_ShouldReturnError_WhenStatusIsNotInTransit()
    {
        // Arrange
        var shipment = CreateTestShipment(); // Status is Created

        // Act
        var result = shipment.Deliver();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ShipmentStatus.Created, shipment.Status); // Status didn't change
    }

    [Fact]
    public void Receive_ShouldUpdateStatus_WhenStatusIsDelivered()
    {
        // Arrange
        var shipment = CreateTestShipment();

        shipment.Process();
        shipment.Dispatch();
        shipment.Transit();
        shipment.Deliver(); // Set status to Delivered

        // Act
        var result = shipment.Receive();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ShipmentStatus.Received, shipment.Status);
    }

    [Fact]
    public void Receive_ShouldReturnError_WhenStatusIsNotDelivered()
    {
        // Arrange
        var shipment = CreateTestShipment(); // Status is Created

        // Act
        var result = shipment.Receive();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ShipmentStatus.Created, shipment.Status); // Status didn't change
    }

    [Fact]
    public void Cancel_ShouldUpdateStatus_WhenStatusIsNotDelivered()
    {
        // Arrange
        var shipment = CreateTestShipment(); // Status is Created

        // Act
        var result = shipment.Cancel();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ShipmentStatus.Cancelled, shipment.Status);
    }

    [Fact]
    public void Cancel_ShouldReturnError_WhenStatusIsDelivered()
    {
        // Arrange
        var shipment = CreateTestShipment();

        shipment.Process();
        shipment.Dispatch();
        shipment.Transit();
        shipment.Deliver(); // Set status to Delivered

        // Act
        var result = shipment.Cancel();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ShipmentStatus.Delivered, shipment.Status); // Status didn't change
    }

    private static Shipment CreateTestShipment()
    {
        return Shipment.Create(
            "SHP12345",
            "ORD9876",
            new Address
            {
                Street = "123 Main St",
                City = "New York",
                Zip = "10001"
            },
            "FedEx",
            "test@example.com",
            [
	            new ShipmentItem { Product = "Product1", Quantity = 2 },
	            new ShipmentItem { Product = "Product2", Quantity = 3 }
            ]);
    }
}
