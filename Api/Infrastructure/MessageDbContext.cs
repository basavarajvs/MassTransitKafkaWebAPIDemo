
using Messages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Api.Domains.OrderProcessing;

namespace Api.Infrastructure
{
    public class MessageDbContext : DbContext
    {
        public MessageDbContext(DbContextOptions<MessageDbContext> options) : base(options)
        {
        }

        public DbSet<Message> Messages { get; set; }
        public DbSet<OrderProcessingSagaState> SagaStates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Message>(entity =>
            {
                // Use Id as primary key
                entity.HasKey(e => e.Id);
                
                // Configure StepData to be stored as JSON
                entity.Property(e => e.StepData)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
                    );
            });

            // Configure Saga State
            modelBuilder.Entity<OrderProcessingSagaState>(entity =>
            {
                entity.HasKey(e => e.CorrelationId);
                
                // Configure complex types as JSON
                entity.Property(e => e.OriginalMessage)
                    .HasConversion(
                        v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<Message>(v, (JsonSerializerOptions?)null)
                    );
                
                entity.Property(e => e.CurrentState).HasMaxLength(50);
                entity.Property(e => e.OriginalMessageJson).HasColumnType("TEXT");
                entity.Property(e => e.OrderCreateResponse).HasColumnType("TEXT");
                entity.Property(e => e.OrderProcessResponse).HasColumnType("TEXT");
                entity.Property(e => e.OrderShipResponse).HasColumnType("TEXT");
                entity.Property(e => e.LastError).HasColumnType("TEXT");
            });
        }
    }
}
