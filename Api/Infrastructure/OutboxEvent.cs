namespace Api.Infrastructure
{
    /// <summary>
    /// Outbox Event entity - stores commands/events for guaranteed delivery
    /// 
    /// WHY OUTBOX PATTERN:
    /// - Ensures exactly-once delivery semantics
    /// - Atomic persistence (message + command saved together)
    /// - Survives application restarts without message loss
    /// - Industry standard pattern for distributed systems
    /// - No additional infrastructure required (uses existing SQLite)
    /// 
    /// HOW IT WORKS:
    /// 1. Save original message + outbox event in same transaction (atomic)
    /// 2. Try immediate publish (best effort)
    /// 3. Background processor handles failed/missed publications
    /// 4. Each event processed exactly once (no duplicates)
    /// </summary>
    public class OutboxEvent
    {
        /// <summary>
        /// Unique identifier for the outbox event
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();
        
        /// <summary>
        /// Type of event for deserialization (e.g., "OrderProcessingSagaStarted")
        /// </summary>
        public string EventType { get; set; } = string.Empty;
        
        /// <summary>
        /// JSON serialized event payload
        /// </summary>
        public string Payload { get; set; } = string.Empty;
        
        /// <summary>
        /// When this event should be processed (allows for delayed execution)
        /// </summary>
        public DateTime ScheduledFor { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Whether this event has been successfully processed
        /// </summary>
        public bool Processed { get; set; } = false;
        
        /// <summary>
        /// When this event was successfully processed
        /// </summary>
        public DateTime? ProcessedAt { get; set; }
        
        /// <summary>
        /// Number of retry attempts for failed processing
        /// </summary>
        public int RetryCount { get; set; } = 0;
        
        /// <summary>
        /// Last error message if processing failed
        /// </summary>
        public string? LastError { get; set; }
    }
}