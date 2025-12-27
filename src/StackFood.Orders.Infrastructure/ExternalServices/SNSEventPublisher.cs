using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Logging;
using StackFood.Orders.Application.Interfaces;
namespace StackFood.Orders.Infrastructure.ExternalServices;
public class SNSEventPublisher : IEventPublisher
{
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly Dictionary<string, string> _topicArns;
    private readonly ILogger<SNSEventPublisher> _logger;

    public SNSEventPublisher(IAmazonSimpleNotificationService snsClient, Dictionary<string, string> topicArns, ILogger<SNSEventPublisher> logger)
    {
        _snsClient = snsClient;
        _topicArns = topicArns;
        _logger = logger;
    }
    public async Task PublishAsync<T>(string topic, T @event) where T : class
    {
        try
        {
            _logger.LogInformation("Publishing event to topic {Topic}", topic);

            if (!_topicArns.TryGetValue(topic, out var topicArn))
            {
                _logger.LogError("Topic {Topic} not configured in topic ARNs", topic);
                throw new ArgumentException($"Topic {topic} not configured");
            }

            _logger.LogInformation("Topic ARN: {TopicArn}", topicArn);
            var message = JsonSerializer.Serialize(@event);
            _logger.LogInformation("Message payload: {Message}", message);

            var response = await _snsClient.PublishAsync(new PublishRequest
            {
                TopicArn = topicArn,
                Message = message
            });

            _logger.LogInformation("Successfully published event to SNS. MessageId: {MessageId}", response.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event to topic {Topic}", topic);
            throw;
        }
    }
}
