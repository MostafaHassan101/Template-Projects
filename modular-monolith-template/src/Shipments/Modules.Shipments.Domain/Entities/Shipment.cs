using Modules.Common.Domain.Results;
using Modules.Shipments.Domain.Enums;
using Modules.Shipments.Domain.ValueObjects;

namespace Modules.Shipments.Domain.Entities;

public sealed class Shipment
{
	private const string ErrorCode = "Shipments.Validation";

	private readonly List<ShipmentItem> _items = [];

	public Guid Id { get; private init; }

	public string Number { get; private set; } = null!;

	public string OrderId { get; private set; } = null!;

	public Address Address { get; private set; } = null!;

	public string Carrier { get; private set; } = null!;

	public string ReceiverEmail { get; private set; } = null!;

	public ShipmentStatus Status { get; private set; }

	public IReadOnlyList<ShipmentItem> Items => _items.AsReadOnly();

	public DateTime CreatedAt { get; private set; }

	public DateTime? UpdatedAt { get; private set; }

	private Shipment()
	{
	}

	public static Shipment Create(
		string number,
		string orderId,
		Address address,
		string carrier,
		string receiverEmail,
		List<ShipmentItem> items)
	{
		var shipment = new Shipment
		{
			Id = Guid.NewGuid(),
			Number = number,
			OrderId = orderId,
			Address = address,
			Carrier = carrier,
			ReceiverEmail = receiverEmail,
			Status = ShipmentStatus.Created,
			CreatedAt = DateTime.UtcNow
		};

		shipment.AddItems(items);

		return shipment;
	}

	public void AddItem(ShipmentItem item)
	{
		_items.Add(item);
		UpdatedAt = DateTime.UtcNow;
	}

	public void AddItems(List<ShipmentItem> items)
	{
		_items.AddRange(items);
		UpdatedAt = DateTime.UtcNow;
	}

	public void RemoveItem(ShipmentItem item)
	{
		_items.Remove(item);
		UpdatedAt = DateTime.UtcNow;
	}

	public void UpdateAddress(Address newAddress)
	{
		Address = newAddress;
		UpdatedAt = DateTime.UtcNow;
	}

	public Result<Success> Process()
	{
		if (Status is not ShipmentStatus.Created)
		{
			return Error.Validation(ErrorCode, $"Can only update to Processing from Created status for shipment {Number}");
		}

		Status = ShipmentStatus.Processing;
		UpdatedAt = DateTime.UtcNow;

		return Result.Success;
	}

	public Result<Success> Dispatch()
	{
		if (Status is not ShipmentStatus.Processing)
		{
			return Error.Validation(ErrorCode, $"Can only update to Dispatched from Processing status for shipment {Number}");
		}

		Status = ShipmentStatus.Dispatched;
		UpdatedAt = DateTime.UtcNow;

		return Result.Success;
	}

	public Result<Success> Transit()
	{
		if (Status is not ShipmentStatus.Dispatched)
		{
			return Error.Validation(ErrorCode, $"Can only update to InTransit from Dispatched status for shipment {Number}");
		}

		Status = ShipmentStatus.InTransit;
		UpdatedAt = DateTime.UtcNow;

		return Result.Success;
	}

	public Result<Success> Deliver()
	{
		if (Status is not ShipmentStatus.InTransit)
		{
			return Error.Validation(ErrorCode, $"Can only update to Delivered from InTransit status for shipment {Number}");
		}

		Status = ShipmentStatus.Delivered;
		UpdatedAt = DateTime.UtcNow;

		return Result.Success;
	}

	public Result<Success> Receive()
	{
		if (Status is not ShipmentStatus.Delivered)
		{
			return Error.Validation(ErrorCode, $"Can only update to Received from Delivered status for shipment {Number}");
		}

		Status = ShipmentStatus.Received;
		UpdatedAt = DateTime.UtcNow;

		return Result.Success;
	}

	public Result<Success> Cancel()
	{
		if (Status is ShipmentStatus.Delivered)
		{
			return Error.Validation(ErrorCode, $"Cannot cancel a Delivered shipment {Number}");
		}

		Status = ShipmentStatus.Cancelled;
		UpdatedAt = DateTime.UtcNow;

		return Result.Success;
	}
}
