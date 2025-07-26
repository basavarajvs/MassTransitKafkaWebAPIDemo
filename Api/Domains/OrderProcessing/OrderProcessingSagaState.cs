using MassTransit;
using Messages;

namespace Api.Domains.OrderProcessing
{
    /// <summary>
    /// Order Processing Saga State - Domain-specific state for order processing workflow
    /// </summary>
    public class OrderProcessingSagaState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = string.Empty;

        // Original message data
        public Message? OriginalMessage { get; set; }
        public string? OriginalMessageJson { get; set; }

        // Order Create Step
        public int OrderCreateRetryCount { get; set; }
        public bool OrderCreatedApiCalled { get; set; }
        public string? OrderCreateResponse { get; set; }

        // Order Process Step
        public int OrderProcessRetryCount { get; set; }
        public bool OrderProcessedApiCalled { get; set; }
        public string? OrderProcessResponse { get; set; }

        // Order Ship Step
        public int OrderShipRetryCount { get; set; }
        public bool OrderShippedApiCalled { get; set; }
        public string? OrderShipResponse { get; set; }

        // Timestamps and error tracking
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string? LastError { get; set; }
    }
} 