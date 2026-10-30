using BGT.Order.Application.Interfaces;
using BGT.Order.Application.Models;
using BGT.Order.Application.Services;

namespace BGT.Order.Tests;

public class OrderServiceTests
{
    [Fact]
    public async Task CreateOrderAsync_WhenRequestIsValid_CachesOrderAndPublishesMessage()
    {
        var redis = new FakeRedisCacheService();
        var rabbit = new FakeRabbitMqPublisher();
        var service = new OrderService(redis, rabbit);

        var request = new CreateOrderRequest
        {
            CustomerId = 42,
            Items =
            [
                new CreateOrderItemRequest { ProductId = 1, Quantity = 2 }
            ]
        };

        var response = await service.CreateOrderAsync(request);

        Assert.Equal(42, response.CustomerId);
        Assert.Single(response.Items);
        Assert.True(redis.WasSet);
        Assert.Equal("bgt.order.created", rabbit.QueueName);
    }

    private sealed class FakeRedisCacheService : IRedisCacheService
    {
        public bool WasSet { get; private set; }

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) => Task.FromResult<T?>(default);

        public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
        {
            WasSet = true;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeRabbitMqPublisher : IRabbitMqPublisher
    {
        public string? QueueName { get; private set; }

        public Task PublishAsync<T>(string queueName, T message, CancellationToken cancellationToken = default)
        {
            QueueName = queueName;
            return Task.CompletedTask;
        }
    }
}
