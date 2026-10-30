using BGT.Order.Application.Interfaces;
using BGT.Order.Application.Models;
using OrderEntity = BGT.Order.Domain.Entities.Order;

namespace BGT.Order.Application.Services;

public class OrderService : IOrderService
{
    private readonly IRedisCacheService _redisCacheService;
    private readonly IRabbitMqPublisher _rabbitMqPublisher;

    public OrderService(IRedisCacheService redisCacheService, IRabbitMqPublisher rabbitMqPublisher)
    {
        _redisCacheService = redisCacheService;
        _rabbitMqPublisher = rabbitMqPublisher;
    }

    public Task<IEnumerable<OrderEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<OrderEntity>>(Array.Empty<OrderEntity>());
    }

    public Task<OrderEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<OrderEntity?>(null);
    }

    public async Task<OrderEntity> CreateAsync(OrderEntity order, CancellationToken cancellationToken = default)
    {
        await _redisCacheService.SetAsync($"orders:{order.Id}", order, cancellationToken: cancellationToken);
        await _rabbitMqPublisher.PublishAsync("bgt.order.created", order, cancellationToken);

        return order;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public async Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.CustomerId <= 0)
        {
            throw new InvalidOperationException("CustomerId must be greater than zero.");
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            throw new InvalidOperationException("At least one order item is required.");
        }

        var order = new OrderEntity
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{request.CustomerId}",
            CustomerName = $"Customer-{request.CustomerId}",
            TotalAmount = 0m,
            CreatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        var itemResponses = new List<CreateOrderItemResponse>();
        decimal totalAmount = 0m;

        foreach (var item in request.Items)
        {
            if (item.ProductId <= 0)
            {
                throw new InvalidOperationException("ProductId must be greater than zero.");
            }

            if (item.Quantity <= 0)
            {
                throw new InvalidOperationException($"Quantity for product {item.ProductId} must be greater than zero.");
            }

            var unitPrice = 0m;
            var lineTotal = unitPrice * item.Quantity;
            totalAmount += lineTotal;

            itemResponses.Add(new CreateOrderItemResponse(
                item.ProductId,
                item.Quantity,
                unitPrice,
                lineTotal));
        }

        order.TotalAmount = totalAmount;

        await _redisCacheService.SetAsync($"orders:{order.Id}", order, cancellationToken: cancellationToken);
        await _rabbitMqPublisher.PublishAsync("bgt.order.created", order, cancellationToken);

        var orderId = GetStableOrderId(order.Id);

        return new CreateOrderResponse(
            orderId,
            request.CustomerId,
            "Pending",
            totalAmount,
            DateTimeOffset.UtcNow,
            itemResponses);
    }

    private static long GetStableOrderId(Guid id)
    {
        var raw = BitConverter.ToInt64(id.ToByteArray(), 0);
        return raw == long.MinValue ? long.MaxValue : Math.Abs(raw);
    }
}
