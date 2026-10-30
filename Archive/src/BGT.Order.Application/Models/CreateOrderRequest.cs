namespace BGT.Order.Application.Models;

/// <summary>
/// Payload for creating a new order.
/// </summary>
public sealed record CreateOrderRequest
{
    public int CustomerId { get; init; }
    public required List<CreateOrderItemRequest> Items { get; init; }
}

/// <summary>
/// A single requested item in an order.
/// </summary>
public sealed record CreateOrderItemRequest
{
    public int ProductId { get; init; }
    public int Quantity { get; init; }
}

/// <summary>
/// Response for the created order.
/// </summary>
public sealed record CreateOrderResponse(
    long OrderId,
    int CustomerId,
    string OrderStatus,
    decimal TotalAmount,
    DateTimeOffset CreatedAt,
    IReadOnlyList<CreateOrderItemResponse> Items);

/// <summary>
/// A single item returned in the created order response.
/// </summary>
public sealed record CreateOrderItemResponse(
    int ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);
