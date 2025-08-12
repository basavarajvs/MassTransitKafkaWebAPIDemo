using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Messages;

namespace Api.Domains.OrderProcessing
{
    /// <summary>
    /// Domain-specific DbContext for Order Processing saga state management.
    /// Separated from infrastructure MessageDbContext to maintain proper domain boundaries.
    /// </summary>
    public class OrderProcessingDbContext : DbContext
    {
        public OrderProcessingDbContext(DbContextOptions<OrderProcessingDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Order Processing saga states - domain-specific entity
        /// </summary>
        public DbSet<OrderProcessingSagaState> SagaStates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureOrderProcessingSagaState(modelBuilder);
        }

        /// <summary>
        /// Configure Order Processing Saga State entity mapping.
        /// Domain-specific configuration for saga state persistence.
        /// </summary>
        private static void ConfigureOrderProcessingSagaState(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderProcessingSagaState>(entity =>
            {
                // Primary key for saga instance tracking
                entity.HasKey(e => e.CorrelationId);
                
                // Configure original message as JSON for data preservation
                entity.Property(e => e.OriginalMessage)
                    .HasConversion(
                        v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<Message>(v, (JsonSerializerOptions?)null)
                    );
                
                // String length limits for performance and storage efficiency
                entity.Property(e => e.CurrentState).HasMaxLength(50);
                
                // TEXT columns for potentially large response data
                entity.Property(e => e.OriginalMessageJson).HasColumnType("TEXT");
                entity.Property(e => e.OrderCreateResponse).HasColumnType("TEXT");
                entity.Property(e => e.OrderProcessResponse).HasColumnType("TEXT");
                entity.Property(e => e.OrderShipResponse).HasColumnType("TEXT");
                entity.Property(e => e.LastError).HasColumnType("TEXT");
                
                // Table naming convention for domain separation
                entity.ToTable("OrderProcessingSagaStates");
            });
        }
    }
}
