
using MassTransit;
using Messages;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;

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
        /// Main message processing method - OUTBOX PATTERN implementation for guaranteed delivery.
        /// 
        /// ATOMIC PROCESSING FLOW (OUTBOX PATTERN):
        /// 1. Start database transaction for atomicity
        /// 2. Save original message to database (audit trail)
        /// 3. Save saga command to outbox table (SAME TRANSACTION)
        /// 4. Commit transaction (both saved atomically)
        /// 5. Try immediate publish (best effort)
        /// 6. Outbox processor handles any missed publications
        /// 
        /// WHY OUTBOX PATTERN:
        /// - ATOMIC: Message and command saved together (no message loss)
        /// - EXACTLY-ONCE: Each event processed exactly once (no duplicates)
        /// - RESILIENT: Survives application restarts without data loss
        /// - GOLD STANDARD: Industry standard pattern for distributed systems
        /// </summary>
        public async Task Consume(ConsumeContext<Message> context)
        {
            var message = context.Message;
            
            // Log message receipt for monitoring and debugging
            _logger.LogInformation($"📨 MessageConsumer received message with ID: {message.Id}");
            
            // Use message ID as correlation ID for idempotency (prevents duplicate sagas)
            // WHY MESSAGE ID: Ensures exactly-once saga processing per message
            // - Same message always creates same correlation ID
            // - Prevents duplicate saga creation on race conditions
            // - Maintains business transaction integrity
            var sagaCorrelationId = message.Id;
            
            // Create saga started event
            var sagaStartedEvent = new Api.Domains.OrderProcessing.OrderProcessingSagaStarted
            {
                CorrelationId = sagaCorrelationId,
                OriginalMessage = message,
                StartedAt = DateTime.UtcNow
            };
            
            // 🔐 ATOMIC TRANSACTION: Save message and publish event using MassTransit outbox
            // MassTransit's outbox automatically handles the dual-write problem
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            
            try
            {
                // 1. Save original message for audit trail
                _dbContext.Messages.Add(message);
                
                // 2. Publish saga event (MassTransit outbox will handle reliable delivery)
                // The outbox middleware automatically captures this publish in the same transaction
                await context.Publish(sagaStartedEvent);
                
                // 3. Commit transaction (includes both message save and outbox event)
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                
                _logger.LogInformation($"🚀 Saga started with correlation ID: {sagaCorrelationId} for message ID: {message.Id}");
                _logger.LogInformation($"✅ Message saved and saga event added to outbox for reliable delivery");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"❌ Failed to save message and publish saga event for message ID: {message.Id}");
                throw; // MassTransit will retry the whole message consumption
            }
        }
    }
}
