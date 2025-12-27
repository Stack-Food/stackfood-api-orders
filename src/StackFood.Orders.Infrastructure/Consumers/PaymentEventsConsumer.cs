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
public class PaymentEventsConsumer : SqsEventsConsumerBase<PaymentEventMessage>
{
    public PaymentEventsConsumer(
        IAmazonSQS sqsClient,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<PaymentEventsConsumer> logger)
        : base(
            sqsClient,
            serviceProvider,
            configuration,
            logger,
            "AWS:SQS:PaymentEventsQueueUrl",
            "http://localhost:4566/000000000000/sqs-orders-payment-events")
    {
    }

    public override async Task ProcessMessageAsync(Message message)
    {
        // SNS wraps the message in a JSON envelope
        var snsMessage = JsonSerializer.Deserialize<SnsMessageWrapper>(message.Body);
        if (snsMessage?.Message == null)
        {
            Logger.LogWarning("Invalid SNS message format: {Body}", message.Body);
            return;
        }

        // Deserialize the actual payment event
        var paymentEvent = JsonSerializer.Deserialize<PaymentEventMessage>(snsMessage.Message, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (paymentEvent == null)
        {
            Logger.LogWarning("Failed to deserialize payment event: {Message}", snsMessage.Message);
            return;
        }

        using var scope = ServiceProvider.CreateScope();
        var updateStatusUseCase = scope.ServiceProvider.GetRequiredService<UpdateOrderStatusUseCase>();

        switch (paymentEvent.EventType?.ToLower())
        {
            case "paymentapproved":
                await updateStatusUseCase.ApprovePaymentAsync(paymentEvent.OrderId);
                Logger.LogInformation("Payment approved for order {OrderId}", paymentEvent.OrderId);
                break;

            case "paymentrejected":
            case "paymentcancelled":
                await updateStatusUseCase.RejectPaymentAsync(paymentEvent.OrderId, paymentEvent.Reason ?? "Payment rejected");
                Logger.LogInformation("Payment rejected for order {OrderId}", paymentEvent.OrderId);
                break;

            default:
                Logger.LogWarning("Unknown payment event type: {EventType} for order {OrderId}",
                    paymentEvent.EventType, paymentEvent.OrderId);
                break;
        }
    }
}

public class PaymentEventMessage
{
    public string? EventType { get; set; }
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public string? Reason { get; set; }
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
