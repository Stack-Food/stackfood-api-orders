using System.Diagnostics.CodeAnalysis;

namespace StackFood.Orders.Infrastructure.Consumers;

/// <summary>
/// Helper class to deserialize SNS message wrapper
/// </summary>
[ExcludeFromCodeCoverage]
public class SnsMessageWrapper
{
    public string? Message { get; set; }
    public string? MessageId { get; set; }
    public string? TopicArn { get; set; }
    public string? Type { get; set; }
}
