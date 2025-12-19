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
        var paymentEvent = JsonSerializer.Deserialize<PaymentEventMessage>(message.Body);
        if (paymentEvent == null)
        {
            Logger.LogWarning("Failed to deserialize payment event: {Body}", message.Body);
            return;
        }

        using var scope = ServiceProvider.CreateScope();
        var updateStatusUseCase = scope.ServiceProvider.GetRequiredService<UpdateOrderStatusUseCase>();

        switch (paymentEvent.Status?.ToLower())
        {
            case "approved":
                await updateStatusUseCase.ApprovePaymentAsync(paymentEvent.OrderId);
                Logger.LogInformation("Payment approved for order {OrderId}", paymentEvent.OrderId);
                break;

            case "rejected":
            case "cancelled":
                await updateStatusUseCase.RejectPaymentAsync(paymentEvent.OrderId, paymentEvent.Reason ?? "Payment rejected");
                Logger.LogInformation("Payment rejected for order {OrderId}", paymentEvent.OrderId);
                break;

            default:
                Logger.LogWarning("Unknown payment status: {Status} for order {OrderId}",
                    paymentEvent.Status, paymentEvent.OrderId);
                break;
        }
    }
}

public class PaymentEventMessage
{
    public Guid OrderId { get; set; }
    public string? Status { get; set; }
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; }
}
