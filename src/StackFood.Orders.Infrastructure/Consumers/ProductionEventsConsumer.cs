using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using StackFood.Orders.Application.UseCases;
using System.Text.Json;

namespace StackFood.Orders.Infrastructure.Consumers;

public class ProductionEventsConsumer : BackgroundService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProductionEventsConsumer> _logger;
    private readonly string _queueUrl;

    public ProductionEventsConsumer(
        IAmazonSQS sqsClient,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<ProductionEventsConsumer> logger)
    {
        _sqsClient = sqsClient;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _queueUrl = configuration["AWS:SQS:ProductionEventsQueueUrl"]
            ?? "http://localhost:4566/000000000000/sqs-orders-production-events";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Production Events Consumer started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var receiveRequest = new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 20
                };

                var response = await _sqsClient.ReceiveMessageAsync(receiveRequest, stoppingToken);

                foreach (var message in response.Messages)
                {
                    try
                    {
                        await ProcessMessageAsync(message);

                        await _sqsClient.DeleteMessageAsync(new DeleteMessageRequest
                        {
                            QueueUrl = _queueUrl,
                            ReceiptHandle = message.ReceiptHandle
                        }, stoppingToken);

                        _logger.LogInformation("Production event processed: {MessageId}", message.MessageId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing production event {MessageId}", message.MessageId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving messages from production events queue");
                await Task.Delay(5000, stoppingToken);
            }
        }

        _logger.LogInformation("Production Events Consumer stopped");
    }

    private async Task ProcessMessageAsync(Message message)
    {
        var productionEvent = JsonSerializer.Deserialize<ProductionEventMessage>(message.Body);
        if (productionEvent == null)
        {
            _logger.LogWarning("Failed to deserialize production event: {Body}", message.Body);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var updateStatusUseCase = scope.ServiceProvider.GetRequiredService<UpdateOrderStatusUseCase>();

        switch (productionEvent.EventType?.ToLower())
        {
            case "productionstarted":
                await updateStatusUseCase.StartProductionAsync(productionEvent.OrderId);
                _logger.LogInformation("Production started for order {OrderId}", productionEvent.OrderId);
                break;

            case "productionready":
                await updateStatusUseCase.MarkAsReadyAsync(productionEvent.OrderId);
                _logger.LogInformation("Order {OrderId} is ready for pickup", productionEvent.OrderId);
                break;

            case "productiondelivered":
                await updateStatusUseCase.CompleteOrderAsync(productionEvent.OrderId);
                _logger.LogInformation("Order {OrderId} has been delivered", productionEvent.OrderId);
                break;

            default:
                _logger.LogWarning("Unknown production event type: {EventType} for order {OrderId}",
                    productionEvent.EventType, productionEvent.OrderId);
                break;
        }
    }
}

public class ProductionEventMessage
{
    public string? EventType { get; set; }
    public Guid OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public DateTime Timestamp { get; set; }
}
