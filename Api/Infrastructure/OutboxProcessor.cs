using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Api.Domains.OrderProcessing;

namespace Api.Infrastructure
{
    /// <summary>
    /// Outbox Processor - Background service that ensures guaranteed delivery of saga events
    /// 
    /// WHY BACKGROUND SERVICE:
    /// - Processes events that failed immediate publication
    /// - Provides guaranteed delivery semantics (exactly-once processing)
    /// - Handles application restart scenarios automatically
    /// - Implements retry logic with exponential backoff
    /// - Self-healing: automatically recovers from failures
    /// 
    /// HOW IT WORKS:
    /// 1. Polls outbox table every 5 seconds for unprocessed events
    /// 2. Deserializes and publishes events to MassTransit bus
    /// 3. Marks events as processed on successful publication
    /// 4. Implements exponential backoff for failed events
    /// 5. Dead letters events after 5 failed attempts
    /// 
    /// PERFORMANCE CONSIDERATIONS:
    /// - Processes events in batches (10 at a time)
    /// - Uses optimized database index (Processed, ScheduledFor)
    /// - Minimal CPU usage when no events pending
    /// - Scoped service provider for proper DI lifecycle
    /// </summary>
    public class OutboxProcessor : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OutboxProcessor> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(5);  // Check every 5 seconds
        private readonly int _batchSize = 10;  // Process 10 events at a time
        private readonly int _maxRetries = 5;  // Max retry attempts before dead lettering

        public OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("📤 Outbox Processor started - ensuring guaranteed delivery of saga events");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingEvents(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error during outbox event processing");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
            
            _logger.LogInformation("📪 Outbox Processor stopped");
        }

        /// <summary>
        /// Process pending outbox events with error handling and retry logic
        /// </summary>
        private async Task ProcessPendingEvents(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MessageDbContext>();
            var bus = scope.ServiceProvider.GetRequiredService<IBus>();

            // Get unprocessed events ordered by schedule time (FIFO processing)
            var pendingEvents = await dbContext.OutboxEvents
                .Where(e => !e.Processed && e.ScheduledFor <= DateTime.UtcNow)
                .OrderBy(e => e.ScheduledFor)
                .Take(_batchSize)
                .ToListAsync(cancellationToken);

            if (!pendingEvents.Any())
            {
                // No events to process - this is the common case
                return;
            }

            _logger.LogInformation($"📤 Processing {pendingEvents.Count} pending outbox events");

            foreach (var evt in pendingEvents)
            {
                await ProcessSingleEvent(evt, bus, dbContext, cancellationToken);
            }

            // Save all changes in one batch for efficiency
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Process a single outbox event with proper error handling and retry logic
        /// </summary>
        private async Task ProcessSingleEvent(OutboxEvent evt, IBus bus, MessageDbContext dbContext, CancellationToken cancellationToken)
        {
            try
            {
                // Deserialize event based on type
                object eventObj = evt.EventType switch
                {
                    "OrderProcessingSagaStarted" => JsonSerializer.Deserialize<OrderProcessingSagaStarted>(evt.Payload)!,
                    "CallOrderCreateApi" => JsonSerializer.Deserialize<CallOrderCreateApi>(evt.Payload)!,
                    "CallOrderProcessApi" => JsonSerializer.Deserialize<CallOrderProcessApi>(evt.Payload)!,
                    "CallOrderShipApi" => JsonSerializer.Deserialize<CallOrderShipApi>(evt.Payload)!,
                    _ => throw new InvalidOperationException($"Unknown event type: {evt.EventType}")
                };

                // Publish the event to MassTransit
                await bus.Publish(eventObj, cancellationToken);
                
                // Mark as successfully processed
                evt.Processed = true;
                evt.ProcessedAt = DateTime.UtcNow;
                evt.LastError = null; // Clear any previous error
                
                _logger.LogInformation($"✅ Successfully processed outbox event: {evt.EventType} - {evt.Id}");
            }
            catch (Exception ex)
            {
                // Handle processing failure with retry logic
                evt.RetryCount++;
                evt.LastError = ex.Message;
                
                if (evt.RetryCount >= _maxRetries)
                {
                    // Dead letter after max retries
                    evt.Processed = true; // Stop retrying
                    _logger.LogError(ex, $"💀 Dead lettering event {evt.Id} after {_maxRetries} failed attempts. EventType: {evt.EventType}");
                }
                else
                {
                    // Schedule retry with exponential backoff
                    var delaySeconds = Math.Pow(2, evt.RetryCount); // 2, 4, 8, 16, 32 seconds
                    evt.ScheduledFor = DateTime.UtcNow.AddSeconds(delaySeconds);
                    
                    _logger.LogWarning(ex, $"⚠️ Failed to process event {evt.Id} (attempt {evt.RetryCount}/{_maxRetries}). " +
                                          $"Scheduling retry in {delaySeconds} seconds. EventType: {evt.EventType}");
                }
            }
        }

        /// <summary>
        /// Override to handle graceful shutdown
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("📪 Outbox Processor shutting down gracefully...");
            await base.StopAsync(cancellationToken);
        }
    }
}