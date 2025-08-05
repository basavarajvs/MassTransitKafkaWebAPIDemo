using MassTransit;
using Microsoft.EntityFrameworkCore;
using Messages;
using Api.Infrastructure;
using Api.Domains.OrderProcessing.SagaSteps;
using Confluent.Kafka;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<MessageDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add HttpClient for API calls
// Add HttpClient for external API calls from saga consumers
// WHY HTTPCLIENT: Required for CallOrderCreateApiConsumer, etc. to make HTTP calls
// Uses HttpClientFactory pattern for proper connection pooling and lifecycle management
builder.Services.AddHttpClient();

// Register saga step classes for dependency injection (as Singleton to match saga lifecycle)
// WHY SINGLETON: Saga state machines are singletons, and steps contain no state
// Steps are essentially stateless command factories and can be safely shared
// This also improves performance by avoiding repeated reflection in GenericStepFactory
builder.Services.AddSingleton<Api.Domains.OrderProcessing.SagaSteps.OrderCreateStep>();
builder.Services.AddSingleton<Api.Domains.OrderProcessing.SagaSteps.OrderProcessStep>();
builder.Services.AddSingleton<Api.Domains.OrderProcessing.SagaSteps.OrderShipStep>();

// Register Outbox Processor for guaranteed delivery pattern
// WHY HOSTED SERVICE: Runs in background to process failed/missed event publications
// Ensures exactly-once delivery semantics for saga events
// Provides automatic recovery from application restarts
builder.Services.AddHostedService<Api.Infrastructure.OutboxProcessor>();

// Configure MassTransit for message processing and saga orchestration
builder.Services.AddMassTransit(x =>
{
    // Register the Order Processing Saga with persistent state storage
    // WHY ENTITY FRAMEWORK REPOSITORY: 
    // - Saga state survives application restarts
    // - Enables exactly-once processing semantics
    // - Provides full audit trail of saga state transitions
    // - Supports concurrent saga execution with optimistic concurrency
    x.AddSagaStateMachine<Api.Domains.OrderProcessing.OrderProcessingSaga, Api.Domains.OrderProcessing.OrderProcessingSagaState>()
        .EntityFrameworkRepository(r =>
        {
            // Use existing DbContext for saga state persistence
            r.ExistingDbContext<MessageDbContext>();
            
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
    // WHY IN-MEMORY: Saga events/commands are internal to this service
    // External communication (Kafka) is handled separately below
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
