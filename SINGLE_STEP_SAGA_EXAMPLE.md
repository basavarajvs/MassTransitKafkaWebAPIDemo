# Single-Step Saga Example

This document demonstrates how to implement a single-step saga using the Factory Interface Pattern framework.

## Overview

The framework supports both multi-step and single-step sagas. For single API calls, sagas provide benefits like:
- **Guaranteed Delivery**: Outbox pattern ensures API calls complete even after restarts
- **Retry Logic**: Configurable retry attempts with exponential backoff  
- **Audit Trail**: Complete tracking of API calls and responses
- **Timeout Handling**: Configurable timeouts per API call
- **Error Handling**: Structured error handling and logging

## Example: Email Notification Saga

Let's implement a saga that sends a single email notification.

### 1. Domain Constants

```csharp
// Api/Domains/EmailNotification/EmailDomainConstants.cs
namespace Api.Domains.EmailNotification
{
    public static class EmailDomainConstants
    {
        public static class StepKeys
        {
            public const string EmailSend = "email-send";
        }
    }
}
```

### 2. Saga State

```csharp
// Api/Domains/EmailNotification/EmailNotificationSagaState.cs
using MassTransit;

namespace Api.Domains.EmailNotification
{
    public class EmailNotificationSagaState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string? CurrentState { get; set; }
        public Messages.Message? OriginalMessage { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        // Email-specific properties
        public bool EmailSentApiCalled { get; set; }
        public string? EmailSendResponse { get; set; }
        public int EmailSendRetryCount { get; set; }
        public string? LastError { get; set; }
    }
}
```

### 3. Events and Commands

```csharp
// Api/Domains/EmailNotification/EmailEvents.cs
namespace Api.Domains.EmailNotification
{
    // Saga start event
    public record EmailNotificationSagaStarted
    {
        public Guid CorrelationId { get; init; }
        public Messages.Message OriginalMessage { get; init; } = null!;
        public DateTime StartedAt { get; init; }
    }

    // API command
    public record CallEmailSendApi
    {
        public Guid CorrelationId { get; init; }
        public object EmailData { get; init; } = null!;
        public int RetryCount { get; init; }
    }

    // Response events
    public record EmailSendApiSucceeded
    {
        public Guid CorrelationId { get; init; }
        public string Response { get; init; } = string.Empty;
    }

    public record EmailSendApiFailed
    {
        public Guid CorrelationId { get; init; }
        public string Error { get; init; } = string.Empty;
        public int RetryCount { get; init; }
    }
}
```

### 4. Command Factory

```csharp
// Api/Domains/EmailNotification/CommandFactories/EmailSendCommandFactory.cs
using Api.SagaFramework;

namespace Api.Domains.EmailNotification.CommandFactories
{
    /// <summary>
    /// Command factory for Email Send operations.
    /// Creates CallEmailSendApi commands with proper initialization.
    /// </summary>
    public class EmailSendCommandFactory : ICommandFactory<CallEmailSendApi, object>
    {
        /// <summary>
        /// Create CallEmailSendApi command with the specified parameters.
        /// </summary>
        /// <param name="correlationId">Saga correlation ID for tracking</param>
        /// <param name="data">Email data from message StepData["email-send"]</param>
        /// <param name="retryCount">Current retry attempt number</param>
        /// <returns>Fully initialized CallEmailSendApi command</returns>
        public CallEmailSendApi Create(Guid correlationId, object data, int retryCount = 0)
        {
            return new CallEmailSendApi
            {
                CorrelationId = correlationId,
                EmailData = data,
                RetryCount = retryCount
            };
        }
    }
}
```

### 5. Saga State Machine

```csharp
// Api/Domains/EmailNotification/EmailNotificationSaga.cs
using MassTransit;
using Api.Domains.EmailNotification.CommandFactories;

namespace Api.Domains.EmailNotification
{
    /// <summary>
    /// Single-step saga for email notification workflow.
    /// Uses Factory Interface Pattern for command creation.
    /// </summary>
    public class EmailNotificationSaga : MassTransitStateMachine<EmailNotificationSagaState>
    {
        private readonly ILogger<EmailNotificationSaga> _logger;
        private readonly EmailSendCommandFactory _emailFactory;

        public EmailNotificationSaga(
            ILogger<EmailNotificationSaga> logger,
            EmailSendCommandFactory emailFactory)
        {
            _logger = logger;
            _emailFactory = emailFactory;
            ConfigureEvents();
            ConfigureStates();
            ConfigureWorkflow();
        }

        // Events
        public Event<EmailNotificationSagaStarted> SagaStarted { get; private set; } = null!;
        public Event<EmailSendApiSucceeded> EmailSendSucceeded { get; private set; } = null!;
        public Event<EmailSendApiFailed> EmailSendFailed { get; private set; } = null!;

        // States
        public State WaitingForEmailSend { get; private set; } = null!;
        public State Completed { get; private set; } = null!;
        public State Failed { get; private set; } = null!;

        private void ConfigureEvents()
        {
            Event(() => SagaStarted, x => x.CorrelateById(m => m.Message.CorrelationId));
            Event(() => EmailSendSucceeded, x => x.CorrelateById(m => m.Message.CorrelationId));
            Event(() => EmailSendFailed, x => x.CorrelateById(m => m.Message.CorrelationId));
        }

        private void ConfigureStates() => InstanceState(x => x.CurrentState);

        private void ConfigureWorkflow()
        {
            Initially(
                When(SagaStarted)
                    .Then(context => {
                        context.Saga.CorrelationId = context.Message.CorrelationId;
                        context.Saga.OriginalMessage = context.Message.OriginalMessage;
                        context.Saga.StartedAt = context.Message.StartedAt;
                        context.Saga.LastUpdated = DateTime.UtcNow;
                        _logger.LogInformation($"📧 Email saga started for correlation ID: {context.Saga.CorrelationId}");
                    })
                    .PublishAsync(context => context.Init<CallEmailSendApi>(_emailFactory.Create(context.Saga.CorrelationId, ExtractEmailData(context.Message.OriginalMessage))))
                    .TransitionTo(WaitingForEmailSend)
            );

            During(WaitingForEmailSend,
                When(EmailSendSucceeded)
                    .Then(context => {
                        context.Saga.EmailSentApiCalled = true;
                        context.Saga.EmailSendResponse = context.Message.Response;
                        context.Saga.CompletedAt = DateTime.UtcNow;
                        context.Saga.LastUpdated = DateTime.UtcNow;
                        _logger.LogInformation($"✅ Email sent successfully for correlation ID: {context.Saga.CorrelationId}");
                    })
                    .TransitionTo(Completed)
                    .Finalize(),
                When(EmailSendFailed)
                    .Then(context => {
                        context.Saga.EmailSendRetryCount = context.Message.RetryCount;
                        context.Saga.LastError = context.Message.Error;
                        context.Saga.LastUpdated = DateTime.UtcNow;
                        _logger.LogWarning($"❌ Email send failed (attempt {context.Message.RetryCount}): {context.Message.Error}");
                    })
                    .If(context => ShouldRetryStep(context.Message.RetryCount, 3),
                        x => x.PublishAsync(context => context.Init<CallEmailSendApi>(_emailFactory.Create(context.Saga.CorrelationId, ExtractEmailData(context.Saga.OriginalMessage!), context.Message.RetryCount + 1))))
                    .Else(x => x.TransitionTo(Failed).Finalize())
            );

            SetCompletedWhenFinalized();
        }

        private static object ExtractEmailData(Messages.Message message)
        {
            return message.StepData.TryGetValue(EmailDomainConstants.StepKeys.EmailSend, out var emailData)
                ? emailData
                : new { };
        }

        private static bool ShouldRetryStep(int currentRetryCount, int maxRetries) => currentRetryCount < maxRetries;
    }
}
```

### 6. API Consumer

```csharp
// Api/Domains/EmailNotification/EmailConsumers.cs
using MassTransit;

namespace Api.Domains.EmailNotification
{
    /// <summary>
    /// Consumer that handles email send API calls.
    /// </summary>
    public class CallEmailSendApiConsumer : IConsumer<CallEmailSendApi>
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CallEmailSendApiConsumer> _logger;

        public CallEmailSendApiConsumer(HttpClient httpClient, ILogger<CallEmailSendApiConsumer> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<CallEmailSendApi> context)
        {
            try
            {
                _logger.LogInformation($"📧 Sending email for correlation ID: {context.Message.CorrelationId}");

                // Call email service API
                var response = await _httpClient.PostAsJsonAsync("https://localhost:5001/api/email/send", 
                    context.Message.EmailData);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    await context.Publish(new EmailSendApiSucceeded
                    {
                        CorrelationId = context.Message.CorrelationId,
                        Response = responseContent
                    });
                }
                else
                {
                    await context.Publish(new EmailSendApiFailed
                    {
                        CorrelationId = context.Message.CorrelationId,
                        Error = $"HTTP {response.StatusCode}: {response.ReasonPhrase}",
                        RetryCount = context.Message.RetryCount
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Email send failed for correlation ID: {context.Message.CorrelationId}");
                await context.Publish(new EmailSendApiFailed
                {
                    CorrelationId = context.Message.CorrelationId,
                    Error = ex.Message,
                    RetryCount = context.Message.RetryCount
                });
            }
        }
    }
}
```

### 7. Dependency Injection Configuration

```csharp
// Add to Program.cs
// Register command factory
builder.Services.AddScoped<Api.Domains.EmailNotification.CommandFactories.EmailSendCommandFactory>();

// Configure MassTransit
builder.Services.AddMassTransit(x =>
{
    // Add saga
    x.AddSagaStateMachine<Api.Domains.EmailNotification.EmailNotificationSaga, Api.Domains.EmailNotification.EmailNotificationSagaState>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Optimistic;
            r.AddDbContext<DbContext, MessageDbContext>();
        });

    // Add consumer
    x.AddConsumer<Api.Domains.EmailNotification.CallEmailSendApiConsumer>();
    
    // Configure transport...
});
```

### 8. Database Configuration

```csharp
// Add to MessageDbContext.cs
public DbSet<Api.Domains.EmailNotification.EmailNotificationSagaState> EmailNotificationSagaStates { get; set; } = null!;

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Configure EmailNotificationSagaState
    modelBuilder.Entity<Api.Domains.EmailNotification.EmailNotificationSagaState>(entity =>
    {
        entity.HasKey(e => e.CorrelationId);
        entity.Property(e => e.OriginalMessage)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Messages.Message>(v, (JsonSerializerOptions?)null));
    });
}
```

## Usage

To trigger the email saga, send a message with email data:

```json
{
  "id": "12345678-1234-1234-1234-123456789012",
  "stepData": {
    "email-send": {
      "to": "user@example.com",
      "subject": "Welcome!",
      "body": "Welcome to our platform!"
    }
  }
}
```

## Benefits of Single-Step Sagas

| **Feature** | **Direct API Call** | **Single-Step Saga** |
|-------------|--------------------|-----------------------|
| **Guaranteed Delivery** | ❌ Lost on restart | ✅ Outbox pattern ensures delivery |
| **Retry Logic** | ❌ Manual implementation | ✅ Built-in with exponential backoff |
| **Audit Trail** | ❌ No tracking | ✅ Complete state tracking |
| **Timeout Handling** | ❌ Manual implementation | ✅ Configurable timeouts |
| **Error Handling** | ❌ Basic try-catch | ✅ Structured error handling |
| **Observability** | ❌ Limited logging | ✅ Rich logging and metrics |

## When to Use Single-Step Sagas

✅ **Good for:**
- Critical API calls that must not be lost
- APIs requiring retry logic
- Operations needing audit trails
- External service calls with timeouts
- APIs with complex error handling

❌ **Overkill for:**
- Simple internal operations
- APIs with built-in idempotency
- High-frequency, low-importance calls
- Synchronous user-facing operations