using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using StackFood.Orders.Application.Interfaces;
namespace StackFood.Orders.Infrastructure.ExternalServices;

[ExcludeFromCodeCoverage]
public class SNSEventPublisher : IEventPublisher
{
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly Dictionary<string, string> _topicArns;
    public SNSEventPublisher(IAmazonSimpleNotificationService snsClient, Dictionary<string, string> topicArns)
    {
        _snsClient = snsClient;
        _topicArns = topicArns;
    }
    public async Task PublishAsync<T>(string topic, T @event) where T : class
    {
        if (!_topicArns.TryGetValue(topic, out var topicArn))
            throw new ArgumentException($"Topic {topic} not configured");
        var message = JsonSerializer.Serialize(@event);
        await _snsClient.PublishAsync(new PublishRequest
        {
            TopicArn = topicArn,
            Message = message
        });
    }
}
