using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using StackFood.Orders.Application.UseCases;
using System.Text.Json;

namespace StackFood.Orders.Infrastructure.Consumers;

public class PaymentEventsConsumer : BackgroundService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentEventsConsumer> _logger;
    private readonly string _queueUrl;

    public PaymentEventsConsumer(
        IAmazonSQS sqsClient,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<PaymentEventsConsumer> logger)
    {
        _sqsClient = sqsClient;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _queueUrl = configuration["AWS:SQS:PaymentEventsQueueUrl"]
            ?? "http://localhost:4566/000000000000/sqs-orders-payment-events";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment Events Consumer started");

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

                        _logger.LogInformation("Payment event processed: {MessageId}", message.MessageId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing payment event {MessageId}", message.MessageId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving messages from payment events queue");
                await Task.Delay(5000, stoppingToken);
            }
        }

        _logger.LogInformation("Payment Events Consumer stopped");
    }

    private async Task ProcessMessageAsync(Message message)
    {
        // SNS wraps the message in a JSON envelope
        var snsMessage = JsonSerializer.Deserialize<SnsMessageWrapper>(message.Body);
        if (snsMessage?.Message == null)
        {
            _logger.LogWarning("Invalid SNS message format: {Body}", message.Body);
            return;
        }

        // Deserialize the actual payment event
        var paymentEvent = JsonSerializer.Deserialize<PaymentEventMessage>(snsMessage.Message, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (paymentEvent == null)
        {
            _logger.LogWarning("Failed to deserialize payment event: {Message}", snsMessage.Message);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var updateStatusUseCase = scope.ServiceProvider.GetRequiredService<UpdateOrderStatusUseCase>();

        switch (paymentEvent.EventType?.ToLower())
        {
            case "paymentapproved":
                await updateStatusUseCase.ApprovePaymentAsync(paymentEvent.OrderId);
                _logger.LogInformation("Payment approved for order {OrderId}", paymentEvent.OrderId);
                break;

            case "paymentrejected":
            case "paymentcancelled":
                await updateStatusUseCase.RejectPaymentAsync(paymentEvent.OrderId, paymentEvent.Reason ?? "Payment rejected");
                _logger.LogInformation("Payment rejected for order {OrderId}", paymentEvent.OrderId);
                break;

            default:
                _logger.LogWarning("Unknown payment event type: {EventType} for order {OrderId}",
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
