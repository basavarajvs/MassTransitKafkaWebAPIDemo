using MassTransit;
using Microsoft.EntityFrameworkCore;
using Messages;
using Api.Infrastructure;
using Api.Domains.OrderProcessing.SagaSteps;
using Confluent.Kafka;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Infrastructure context for generic message and outbox storage
// Support both SQLite and PostgreSQL based on connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<MessageDbContext>(options =>
{
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("DefaultConnection connection string is required");
    }
    
    // Auto-detect database provider from connection string
    if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase) || 
        connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) && 
        connectionString.Contains("Port=", StringComparison.OrdinalIgnoreCase))
    {
        // PostgreSQL connection string detected
        options.UseNpgsql(connectionString);
    }
    else
    {
        // Default to SQLite for file-based connections
        options.UseSqlite(connectionString);
    }
});

// Domain-specific context for Order Processing saga state
// WHY SEPARATE CONTEXT: Maintains domain boundaries and separation of concerns
// - MessageDbContext: Infrastructure concerns (messages, outbox events)
// - OrderProcessingDbContext: Domain concerns (saga state)
builder.Services.AddDbContext<Api.Domains.OrderProcessing.OrderProcessingDbContext>(options =>
{
    // Use the same database provider detection logic as MessageDbContext
    if (connectionString!.Contains("Host=", StringComparison.OrdinalIgnoreCase) || 
        connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) && 
        connectionString.Contains("Port=", StringComparison.OrdinalIgnoreCase))
    {
        // PostgreSQL connection string detected
        options.UseNpgsql(connectionString);
    }
    else
    {
        // Default to SQLite for file-based connections
        options.UseSqlite(connectionString);
    }
});

// Add HttpClient for API calls
// Add HttpClient for external API calls from saga consumers
// WHY HTTPCLIENT: Required for CallOrderCreateApiConsumer, etc. to make HTTP calls
// Uses HttpClientFactory pattern for proper connection pooling and lifecycle management
builder.Services.AddHttpClient();

// Note: Step classes no longer need DI registration with Factory Interface Pattern
// Sagas now use injected factories directly for command creation

// MassTransit built-in outbox pattern configuration
// 
// ARCHITECTURAL DECISION: Use MassTransit's Entity Framework Outbox
// WHY CHOSEN: Production-proven outbox implementation with minimal maintenance
// - PROBLEM: Saving business data + publishing events can fail partially
// - SOLUTION: MassTransit handles atomic transactions and delivery automatically
// - RESULT: Zero message loss, exactly-once delivery semantics
//
// BENEFITS OVER CUSTOM IMPLEMENTATION:
// - Battle-tested by thousands of applications
// - Built-in retry logic and error handling
// - Rich diagnostics and monitoring
// - Less code to maintain and debug
// - Configurable polling intervals and delivery limits

// Register command factories for Factory Interface Pattern
// WHY EXPLICIT REGISTRATION: 
// - Type-safe dependency injection
// - Clear visibility of all factories
// - Easy mocking for unit tests
// - No reflection or magic registration
// Register command factories as Singleton to match saga lifetime
// WHY SINGLETON: OrderProcessingSaga is singleton (required by MassTransit state machines)
// Command factories are stateless and safe for singleton use
builder.Services.AddSingleton<Api.Domains.OrderProcessing.CommandFactories.OrderCreateCommandFactory>();
builder.Services.AddSingleton<Api.Domains.OrderProcessing.CommandFactories.OrderProcessCommandFactory>();
builder.Services.AddSingleton<Api.Domains.OrderProcessing.CommandFactories.OrderShipCommandFactory>();

// Configure MassTransit for message processing and saga orchestration
builder.Services.AddMassTransit(x =>
{
    // Configure MassTransit's built-in Entity Framework Outbox
    // WHY OUTBOX PATTERN: Ensures reliable message delivery with database consistency
    x.AddEntityFrameworkOutbox<MessageDbContext>(o =>
    {
        // Auto-detect and configure database provider for outbox
        if (connectionString!.Contains("Host=", StringComparison.OrdinalIgnoreCase) || 
            connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) && 
            connectionString.Contains("Port=", StringComparison.OrdinalIgnoreCase))
        {
            // PostgreSQL outbox configuration
            o.UsePostgres();
        }
        else
        {
            // SQLite outbox configuration
            o.UseSqlite();
        }
        
        // Configure polling settings (simplified - MassTransit handles delivery limits internally)
        o.QueryDelay = TimeSpan.FromSeconds(5);        // Check for messages every 5 seconds
        o.QueryMessageLimit = 100;                     // Max messages per query
        
        // Use built-in delivery service (recommended for most scenarios)
        o.UseBusOutbox();
        
        // Database transaction isolation level (use System.Data.IsolationLevel)
        o.IsolationLevel = System.Data.IsolationLevel.ReadCommitted;
    });

    // Register the Order Processing Saga with persistent state storage
    // WHY ENTITY FRAMEWORK REPOSITORY: 
    // - Saga state survives application restarts
    // - Enables exactly-once processing semantics
    // - Provides full audit trail of saga state transitions
    // - Supports concurrent saga execution with optimistic concurrency
    x.AddSagaStateMachine<Api.Domains.OrderProcessing.OrderProcessingSaga, Api.Domains.OrderProcessing.OrderProcessingSagaState>()
        .EntityFrameworkRepository(r =>
        {
            // Use domain-specific DbContext for saga state persistence
            // WHY DOMAIN CONTEXT: Maintains separation between infrastructure and domain concerns
            r.ExistingDbContext<Api.Domains.OrderProcessing.OrderProcessingDbContext>();
            
            // CRITICAL: Use optimistic concurrency for SQLite compatibility
            // SQLite doesn't support SQL Server-style row locking (WITH UPDLOCK, ROWLOCK)
            // Optimistic concurrency prevents the "near '(': syntax error" exception
            r.ConcurrencyMode = ConcurrencyMode.Optimistic;
        });

    // Register consumers that handle saga commands (CallOrderCreateApi, etc.)
    // WHY SEPARATE CONSUMERS: Decouples saga logic from HTTP API calls
    // Each consumer is responsible for calling one external API and publishing results
    // This allows for different retry policies, timeouts, and error handling per API
    x.AddConsumer<Api.Domains.OrderProcessing.CallOrderCreateApiConsumer>();
    x.AddConsumer<Api.Domains.OrderProcessing.CallOrderProcessApiConsumer>();
    x.AddConsumer<Api.Domains.OrderProcessing.CallOrderShipApiConsumer>();

    // Configure in-memory bus for saga events and commands
    // 
    // ARCHITECTURAL DECISION: In-Memory Bus + MassTransit Outbox Pattern
    // WHY IN-MEMORY: Saga events/commands are internal to this service
    // - Ultra-fast processing (microsecond latency vs. network round-trips)
    // - No additional infrastructure required (no RabbitMQ, ServiceBus, etc.)
    // - Perfect for internal orchestration within a single service boundary
    //
    // WHY MASSTRANSIT OUTBOX: Production-proven reliability with minimal maintenance
    // - MassTransit handles atomic transactions and delivery automatically
    // - Built-in retry logic and error handling
    // - Rich diagnostics and monitoring capabilities
    // - Best of both worlds: Speed + Reliability
    //
    // EXTERNAL COMMUNICATION: Kafka handled separately below (different concerns)
    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });

    x.AddRider(rider =>
    {
        rider.AddConsumer<MessageConsumer>(); // Register consumer with the rider

        rider.UsingKafka((context, k) =>
        {
            k.Host("127.0.0.1:9092");

            k.TopicEndpoint<Null, Message>("my-topic", "my-new-consumer-group", e =>
            {
                e.ConfigureConsumer<MessageConsumer>(context);
            });
        });
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
