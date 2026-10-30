using BGT.Order.Application.Interfaces;
using BGT.Order.Application.Models;
using BGT.Order.Infrastructure.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace BGT.Order.Infrastructure.Services;

public sealed class SqlOrderService : IOrderService
{
    private readonly OrderRepository _orderRepository;

    public SqlOrderService(IConfiguration configuration)
    {
        _orderRepository = new OrderRepository(configuration);
    }

    public async Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.CustomerId <= 0)
        {
            throw new InvalidOperationException("CustomerId must be greater than zero.");
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            throw new InvalidOperationException("At least one order item is required.");
        }

        await using var connection = new SqlConnection(_orderRepository.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        using var transaction = connection.BeginTransaction();

        try
        {
            var customerExists = await _orderRepository.CustomerExistsAsync(connection, transaction, request.CustomerId, cancellationToken);
            if (!customerExists)
            {
                throw new InvalidOperationException($"Customer {request.CustomerId} does not exist.");
            }

            var productLookup = new Dictionary<int, OrderRepository.ProductSnapshot>();
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

                var product = await _orderRepository.GetProductAsync(connection, transaction, item.ProductId, cancellationToken);
                if (product is null)
                {
                    throw new InvalidOperationException($"Product {item.ProductId} does not exist.");
                }

                if (!product.IsActive)
                {
                    throw new InvalidOperationException($"Product {item.ProductId} is not active.");
                }

                if (product.StockQuantity < item.Quantity)
                {
                    throw new InvalidOperationException($"Insufficient stock for product {item.ProductId}.");
                }

                productLookup[item.ProductId] = product;
            }

            decimal totalAmount = 0m;
            var itemResponses = new List<CreateOrderItemResponse>();

            foreach (var item in request.Items)
            {
                var product = productLookup[item.ProductId];
                var lineTotal = product.Price * item.Quantity;
                totalAmount += lineTotal;

                var stockUpdated = await _orderRepository.DeductStockAsync(connection, transaction, item.ProductId, item.Quantity, cancellationToken);
                if (!stockUpdated)
                {
                    throw new InvalidOperationException($"Stock deduction failed for product {item.ProductId}.");
                }

                itemResponses.Add(new CreateOrderItemResponse(
                    item.ProductId,
                    item.Quantity,
                    product.Price,
                    lineTotal));
            }

            var orderId = await _orderRepository.CreateOrderAsync(connection, transaction, request.CustomerId, totalAmount, cancellationToken);

            foreach (var item in request.Items)
            {
                var product = productLookup[item.ProductId];
                await _orderRepository.CreateOrderItemAsync(connection, transaction, orderId, item.ProductId, item.Quantity, product.Price, cancellationToken);
            }

            transaction.Commit();

            return new CreateOrderResponse(
                orderId,
                request.CustomerId,
                "Pending",
                totalAmount,
                DateTimeOffset.UtcNow,
                itemResponses);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public Task<IEnumerable<BGT.Order.Domain.Entities.Order>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<BGT.Order.Domain.Entities.Order>>(new List<BGT.Order.Domain.Entities.Order>());
    }

    public Task<BGT.Order.Domain.Entities.Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<BGT.Order.Domain.Entities.Order?>(null);
    }

    public async Task<BGT.Order.Domain.Entities.Order> CreateAsync(BGT.Order.Domain.Entities.Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        return order;
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
