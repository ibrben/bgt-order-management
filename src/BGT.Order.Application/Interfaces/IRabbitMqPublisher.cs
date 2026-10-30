namespace BGT.Order.Application.Interfaces;

public interface IRabbitMqPublisher
{
    Task PublishAsync<T>(string queueName, T message, CancellationToken cancellationToken = default);
}
