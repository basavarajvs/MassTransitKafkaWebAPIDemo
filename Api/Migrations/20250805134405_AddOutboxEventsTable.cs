using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <summary>
    /// Migration to implement the Outbox Pattern for guaranteed event delivery.
    /// 
    /// OUTBOX PATTERN EXPLAINED:
    /// The outbox pattern ensures exactly-once delivery of domain events by storing them
    /// in the same database transaction as the business data. This prevents the classic
    /// distributed systems problem where business data is saved but events are lost due
    /// to failures between database commit and message publishing.
    /// 
    /// HOW IT SOLVES MESSAGE LOSS:
    /// 1. BEFORE: Save message → Publish event → [APP CRASH] → Event lost forever
    /// 2. AFTER: Save message + outbox event (atomic) → Background processor publishes
    /// 
    /// BENEFITS:
    /// - Exactly-once delivery semantics
    /// - Survives application restarts
    /// - No message loss on failures
    /// - Industry standard pattern
    /// - Zero additional infrastructure required
    /// </summary>
    public partial class AddOutboxEventsTable : Migration
    {
        /// <summary>
        /// Creates the OutboxEvents table with optimized indexes for guaranteed event delivery
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create OutboxEvents table for guaranteed event delivery
            // Each row represents an event that must be published to the message bus
            migrationBuilder.CreateTable(
                name: "OutboxEvents",
                columns: table => new
                {
                    // Unique identifier for each outbox event
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    
                    // Event type for deserialization routing (e.g., "OrderProcessingSagaStarted")
                    // Used by OutboxProcessor to deserialize the correct event type
                    EventType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    
                    // JSON serialized event payload containing all event data
                    // Stores the complete event object for publishing
                    Payload = table.Column<string>(type: "TEXT", nullable: false),
                    
                    // When this event should be processed (allows delayed execution)
                    // Used for retry scheduling with exponential backoff
                    ScheduledFor = table.Column<DateTime>(type: "datetime", nullable: false),
                    
                    // Whether this event has been successfully published
                    // False = pending, True = completed (used by background processor)
                    Processed = table.Column<bool>(type: "INTEGER", nullable: false),
                    
                    // Timestamp when event was successfully processed
                    // Used for auditing and monitoring event processing times
                    ProcessedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    
                    // Number of retry attempts for failed events
                    // Used for exponential backoff and dead letter detection
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    
                    // Last error message if processing failed
                    // Used for debugging and monitoring failed events
                    LastError = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxEvents", x => x.Id);
                });

            // Critical performance index for OutboxProcessor queries
            // Optimizes: "WHERE Processed = false AND ScheduledFor <= NOW() ORDER BY ScheduledFor"
            // This is the most frequent query pattern used by the background processor
            // Index covers both the filter conditions and sort order for maximum efficiency
            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_Processed_ScheduledFor",
                table: "OutboxEvents",
                columns: new[] { "Processed", "ScheduledFor" });
        }

        /// <summary>
        /// Reverses the outbox pattern implementation by dropping the OutboxEvents table
        /// WARNING: This will cause message loss if events are pending in the outbox
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboxEvents");
        }
    }
}
