
using Messages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api.Infrastructure
{
    public class MessageDbContext : DbContext
    {
        public MessageDbContext(DbContextOptions<MessageDbContext> options) : base(options)
        {
        }

        public DbSet<Message> Messages { get; set; }
        public DbSet<OutboxEvent> OutboxEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureMessages(modelBuilder);
            ConfigureOutboxEvents(modelBuilder);
        }

        /// <summary>
        /// Configure Message entity for generic message storage.
        /// Used for storing original Kafka messages before saga processing.
        /// </summary>
        private static void ConfigureMessages(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Message>(entity =>
            {
                // Use Id as primary key for message tracking
                entity.HasKey(e => e.Id);
                
                // Configure StepData as JSON for flexible schema
                entity.Property(e => e.StepData)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
                    );
                
                // Table naming for infrastructure separation
                entity.ToTable("Messages");
            });
        }

        /// <summary>
        /// Configure OutboxEvent entity for guaranteed delivery pattern.
        /// Infrastructure concern for ensuring reliable message processing.
        /// </summary>
        private static void ConfigureOutboxEvents(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboxEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Optimize for common queries: unprocessed events ordered by schedule time
                entity.HasIndex(e => new { e.Processed, e.ScheduledFor })
                    .HasDatabaseName("IX_OutboxEvents_Processed_ScheduledFor");
                
                // Event type for deserialization routing
                entity.Property(e => e.EventType)
                    .HasMaxLength(100)
                    .IsRequired();
                
                // JSON payload storage
                entity.Property(e => e.Payload)
                    .HasColumnType("TEXT")
                    .IsRequired();
                
                // Error tracking for failed events
                entity.Property(e => e.LastError)
                    .HasColumnType("TEXT");
                
                // Ensure timestamps are precise
                entity.Property(e => e.ScheduledFor)
                    .HasColumnType("datetime")
                    .IsRequired();
                
                entity.Property(e => e.ProcessedAt)
                    .HasColumnType("datetime");
                
                // Table naming for infrastructure separation
                entity.ToTable("OutboxEvents");
            });
        }
    }
}
