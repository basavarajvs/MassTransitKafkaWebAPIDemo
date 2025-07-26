using Messages;

namespace Api.Domains.OrderProcessing
{
    /// <summary>
    /// Order Processing Saga Events - Domain-specific events for order processing
    /// </summary>

    // Saga lifecycle events
    public record OrderProcessingSagaStarted
    {
        public required Guid CorrelationId { get; init; }
        public required Message OriginalMessage { get; init; }
        public required DateTime StartedAt { get; init; }
    }

    // Order Create API Commands & Events
    public record CallOrderCreateApi
    {
        public required Guid CorrelationId { get; init; }
        public required object OrderData { get; init; }
        public required int RetryCount { get; init; }
    }

    public record OrderCreateApiSucceeded
    {
        public required Guid CorrelationId { get; init; }
        public required string Response { get; init; }
    }

    public record OrderCreateApiFailed
    {
        public required Guid CorrelationId { get; init; }
        public required string Error { get; init; }
        public required int RetryCount { get; init; }
    }

    // Order Process API Commands & Events
    public record CallOrderProcessApi
    {
        public required Guid CorrelationId { get; init; }
        public required object ProcessData { get; init; }
        public required int RetryCount { get; init; }
    }

    public record OrderProcessApiSucceeded
    {
        public required Guid CorrelationId { get; init; }
        public required string Response { get; init; }
    }

    public record OrderProcessApiFailed
    {
        public required Guid CorrelationId { get; init; }
        public required string Error { get; init; }
        public required int RetryCount { get; init; }
    }

    // Order Ship API Commands & Events
    public record CallOrderShipApi
    {
        public required Guid CorrelationId { get; init; }
        public required object ShipData { get; init; }
        public required int RetryCount { get; init; }
    }

    public record OrderShipApiSucceeded
    {
        public required Guid CorrelationId { get; init; }
        public required string Response { get; init; }
    }

    public record OrderShipApiFailed
    {
        public required Guid CorrelationId { get; init; }
        public required string Error { get; init; }
        public required int RetryCount { get; init; }
    }
} 