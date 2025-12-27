using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace StackFood.Orders.Infrastructure.Consumers;

[ExcludeFromCodeCoverage]
public abstract class SqsEventsConsumerBase<TEventMessage> : BackgroundService
{
    private readonly IAmazonSQS _sqsClient;
    protected IServiceProvider ServiceProvider { get; }
    protected ILogger Logger { get; }
    private readonly string _queueUrl;

    protected SqsEventsConsumerBase(
        IAmazonSQS sqsClient,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger logger,
        string queueUrlConfigKey,
        string defaultQueueUrl)
    {
        _sqsClient = sqsClient;
        ServiceProvider = serviceProvider;
        Logger = logger;
        _queueUrl = configuration[queueUrlConfigKey] ?? defaultQueueUrl;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("{ConsumerName} started", GetType().Name);

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

                        Logger.LogInformation("{ConsumerName} event processed: {MessageId}", GetType().Name, message.MessageId);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error processing {ConsumerName} event {MessageId}", GetType().Name, message.MessageId);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error receiving messages from {ConsumerName} queue", GetType().Name);
                await Task.Delay(5000, stoppingToken);
            }
        }

        Logger.LogInformation("{ConsumerName} stopped", GetType().Name);
    }

    public abstract Task ProcessMessageAsync(Message message);
}
