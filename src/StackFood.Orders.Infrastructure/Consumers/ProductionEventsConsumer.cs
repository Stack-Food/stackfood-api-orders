using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackFood.Orders.Application.UseCases;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace StackFood.Orders.Infrastructure.Consumers;

[ExcludeFromCodeCoverage]
public class ProductionEventsConsumer : SqsEventsConsumerBase<ProductionEventMessage>
{
    public ProductionEventsConsumer(
        IAmazonSQS sqsClient,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<ProductionEventsConsumer> logger)
        : base(
            sqsClient,
            serviceProvider,
            configuration,
            logger,
            "AWS:SQS:ProductionEventsQueueUrl",
            "http://localhost:4566/000000000000/sqs-orders-production-events")
    {
    }

    public override async Task ProcessMessageAsync(Message message)
    {
        var productionEvent = JsonSerializer.Deserialize<ProductionEventMessage>(message.Body);
        if (productionEvent == null)
        {
            Logger.LogWarning("Failed to deserialize production event: {Body}", message.Body);
            return;
        }

        using var scope = ServiceProvider.CreateScope();
        var updateStatusUseCase = scope.ServiceProvider.GetRequiredService<UpdateOrderStatusUseCase>();

        switch (productionEvent.EventType?.ToLower())
        {
            case "productionstarted":
                await updateStatusUseCase.StartProductionAsync(productionEvent.OrderId);
                Logger.LogInformation("Production started for order {OrderId}", productionEvent.OrderId);
                break;

            case "productionready":
                await updateStatusUseCase.MarkAsReadyAsync(productionEvent.OrderId);
                Logger.LogInformation("Order {OrderId} is ready for pickup", productionEvent.OrderId);
                break;

            case "productiondelivered":
                await updateStatusUseCase.CompleteOrderAsync(productionEvent.OrderId);
                Logger.LogInformation("Order {OrderId} has been delivered", productionEvent.OrderId);
                break;

            default:
                Logger.LogWarning("Unknown production event type: {EventType} for order {OrderId}",
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
