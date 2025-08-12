
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureMessages(modelBuilder);
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


    }
}
