
using MassTransit;
using Messages;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Text.Json;

namespace Api.Infrastructure
{
    /// <summary>
    /// Kafka message consumer - entry point for all incoming messages from Kafka topics.
    /// 
    /// WHY SEPARATE INFRASTRUCTURE NAMESPACE:
    /// - Clear separation between infrastructure concerns and domain logic
    /// - Follows Clean Architecture: infrastructure depends on domain, not vice versa
    /// - Easy to swap out Kafka for other message brokers later
    /// - Testable in isolation from domain logic
    /// 
    /// RESPONSIBILITY:
    /// - Receive messages from Kafka
    /// - Persist for audit/replay purposes
    /// - Trigger appropriate domain workflows (sagas)
    /// - Handle infrastructure concerns (logging, error handling)
    /// </summary>
    public class MessageConsumer : IConsumer<Message>
    {
        private readonly MessageDbContext _dbContext;
        private readonly ILogger<MessageConsumer> _logger;

        /// <summary>
        /// Dependency injection of infrastructure dependencies.
        /// 
        /// WHY THESE DEPENDENCIES:
        /// - DbContext: For message persistence and audit trail
        /// - Logger: For observability and debugging in distributed systems
        /// </summary>
        public MessageConsumer(MessageDbContext dbContext, ILogger<MessageConsumer> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Main message processing method - called by MassTransit when messages arrive.
        /// 
        /// PROCESSING FLOW:
        /// 1. Log receipt for observability
        /// 2. Persist message for audit trail and potential replay
        /// 3. Trigger domain-specific saga for business logic orchestration
        /// 4. Log saga initiation for tracking
        /// 
        /// WHY THIS FLOW:
        /// - Persistence first ensures we never lose messages even if saga fails
        /// - Separate correlation ID allows multiple sagas for same message if needed
        /// - Publishing saga event decouples infrastructure from domain logic
        /// </summary>
        public async Task Consume(ConsumeContext<Message> context)
        {
            var message = context.Message;
            
            // Log message receipt for monitoring and debugging
            _logger.LogInformation($"MessageConsumer received message with ID: {message.Id}");
            
            // CRITICAL: Save the original message to database first for audit trail
            // This ensures we never lose messages even if downstream processing fails
            // Enables message replay and debugging in production environments
            _dbContext.Messages.Add(message);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation($"Original message saved to database with {message.StepData.Count} steps");
            
            // Generate new correlation ID for saga tracking (separate from message ID)
            // WHY SEPARATE ID: Allows multiple saga instances for same message if needed
            var sagaCorrelationId = Guid.NewGuid();
            
            // Publish domain event to trigger Order Processing Saga
            // WHY PUBLISH PATTERN: Decouples infrastructure from domain logic
            // Other domains can listen to this event without changing this consumer
            await context.Publish(new Api.Domains.OrderProcessing.OrderProcessingSagaStarted
            {
                CorrelationId = sagaCorrelationId,
                OriginalMessage = message,
                StartedAt = DateTime.UtcNow
            });
            
            // Log saga initiation for end-to-end traceability
            _logger.LogInformation($"🚀 Saga started with correlation ID: {sagaCorrelationId} for message ID: {message.Id}");
        }
    }
}
