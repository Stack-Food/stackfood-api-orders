namespace StackFood.Orders.Application.Interfaces;
public interface IEventPublisher
{
    Task PublishAsync<T>(string topic, T @event) where T : class;
}
