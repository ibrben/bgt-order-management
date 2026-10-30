using BGT.Order.Application.Models;
using OrderEntity = BGT.Order.Domain.Entities.Order;

namespace BGT.Order.Application.Interfaces;

public interface IOrderService
{
    Task<IEnumerable<OrderEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<OrderEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OrderEntity> CreateAsync(OrderEntity order, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
}
