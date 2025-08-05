# Single-Step Saga Example: Email Notification

## 🎯 Use Case: Send Welcome Email

**Scenario:** When a user registers, send them a welcome email via external email service.

## 📦 Complete Implementation (Only ~50 lines!)

### **1. Domain Constants**
```csharp
// Api/Domains/EmailNotification/EmailDomainConstants.cs
namespace Api.Domains.EmailNotification
{
    public static class EmailDomainConstants
    {
        public static class StepKeys
        {
            public const string WelcomeEmail = "welcome-email";
        }
        
        public static class Workflow
        {
            public const int MaxRetries = 3;
            public const int TimeoutSeconds = 10;
        }
        
        public static class ApiEndpoints
        {
            public const string SendEmail = "http://localhost:5027/api/email/send";
        }
    }
}
```

### **2. Saga State (Minimal)**
```csharp
// Api/Domains/EmailNotification/EmailNotificationSagaState.cs
using MassTransit;
using Messages;

namespace Api.Domains.EmailNotification
{
    public class EmailNotificationSagaState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = string.Empty;
        
        // Original message
        public Message? OriginalMessage { get; set; }
        
        // Email Send Step (ONLY ONE STEP!)
        public int EmailSendRetryCount { get; set; }
        public bool EmailSentApiCalled { get; set; }
        public string? EmailSendResponse { get; set; }
        
        // Timestamps
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string? LastError { get; set; }
    }
}
```

### **3. Events (Minimal)**
```csharp
// Api/Domains/EmailNotification/EmailEvents.cs
using Messages;

namespace Api.Domains.EmailNotification
{
    // Saga start event
    public record EmailNotificationSagaStarted
    {
        public required Guid CorrelationId { get; init; }
        public required Message OriginalMessage { get; init; }
        public required DateTime StartedAt { get; init; }
    }

    // Email send command & events
    public record CallSendEmailApi
    {
        public required Guid CorrelationId { get; init; }
        public required object EmailData { get; init; }
        public required int RetryCount { get; init; }
    }

    public record SendEmailApiSucceeded
    {
        public required Guid CorrelationId { get; init; }
        public required string Response { get; init; }
    }

    public record SendEmailApiFailed
    {
        public required Guid CorrelationId { get; init; }
        public required string Error { get; init; }
        public required int RetryCount { get; init; }
    }
}
```

### **4. Single Step Class (Tiny!)**
```csharp
// Api/Domains/EmailNotification/SagaSteps/EmailSendStep.cs
using Messages;
using Api.SagaFramework;

namespace Api.Domains.EmailNotification.SagaSteps
{
    [SagaStep(
        StepName = "EmailSend", 
        MessageKey = EmailDomainConstants.StepKeys.WelcomeEmail, 
        MaxRetries = 3, 
        DataPropertyName = "EmailData"
    )]
    public class EmailSendStep : GenericStepBase<CallSendEmailApi, EmailNotificationSagaState>
    {
        public EmailSendStep(ILogger<EmailSendStep> logger) 
            : base(logger, GenericStepFactory.Create<EmailSendStep, EmailNotificationSagaState>(), "EmailData")
        {
        }

        public override CallSendEmailApi CreateCommand<TMessage>(Guid correlationId, TMessage message, int retryCount = 0)
        {
            return GenericCommandFactory.Create<CallSendEmailApi>(correlationId, ExtractStepData(message), _dataPropertyName, retryCount);
        }
    }
}
```

### **5. Single-Step Saga (Super Simple!)**
```csharp
// Api/Domains/EmailNotification/EmailNotificationSaga.cs
using MassTransit;
using Api.Domains.EmailNotification.SagaSteps;

namespace Api.Domains.EmailNotification
{
    public class EmailNotificationSaga : MassTransitStateMachine<EmailNotificationSagaState>
    {
        private readonly ILogger<EmailNotificationSaga> _logger;
        private readonly EmailSendStep _emailStep;

        public EmailNotificationSaga(ILogger<EmailNotificationSaga> logger, EmailSendStep emailStep)
        {
            _logger = logger;
            _emailStep = emailStep;
            
            ConfigureEvents();
            ConfigureWorkflow();
        }

        // Only ONE state needed!
        public State WaitingForEmailSend { get; private set; } = null!;
        
        public Event<EmailNotificationSagaStarted> SagaStarted { get; private set; } = null!;
        public Event<SendEmailApiSucceeded> EmailSucceeded { get; private set; } = null!;
        public Event<SendEmailApiFailed> EmailFailed { get; private set; } = null!;

        private void ConfigureEvents()
        {
            Event(() => SagaStarted, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => EmailSucceeded, x => x.CorrelateById(context => context.Message.CorrelationId));
            Event(() => EmailFailed, x => x.CorrelateById(context => context.Message.CorrelationId));
        }

        private void ConfigureWorkflow()
        {
            // 🚀 Start: Send email immediately
            Initially(
                When(SagaStarted)
                    .Then(context => {
                        context.Saga.CorrelationId = context.Message.CorrelationId;
                        context.Saga.OriginalMessage = context.Message.OriginalMessage;
                        context.Saga.StartedAt = context.Message.StartedAt;
                        context.Saga.LastUpdated = DateTime.UtcNow;
                        
                        _logger.LogInformation($"📧 Email saga started for correlation ID: {context.Saga.CorrelationId}");
                    })
                    .PublishAsync(context => context.Init<CallSendEmailApi>(_emailStep.CreateCommand(context.Saga.CorrelationId, context.Message.OriginalMessage)))
                    .TransitionTo(WaitingForEmailSend)
            );

            // 📧 Email Send: Success → Complete, Failure → Retry or End
            During(WaitingForEmailSend,
                When(EmailSucceeded)
                    .Then(context => {
                        _emailStep.HandleSuccess(context.Saga, context.Message.Response);
                        context.Saga.CompletedAt = DateTime.UtcNow;
                        _logger.LogInformation($"✅ EMAIL SENT successfully for correlation ID: {context.Saga.CorrelationId}");
                    })
                    .Finalize(), // DONE! Single step complete
                When(EmailFailed)
                    .IfElse(context => _emailStep.HandleFailureAndShouldRetry(context.Saga, context.Message.Error, context.Message.RetryCount),
                        retry => retry.PublishAsync(context => context.Init<CallSendEmailApi>(_emailStep.CreateCommand(context.Saga.CorrelationId, context.Saga.OriginalMessage!, context.Message.RetryCount))),
                        fail => fail.Finalize())
            );

            SetCompletedWhenFinalized();
        }
    }
}
```

### **6. API Consumer (Same Pattern)**
```csharp
// Api/Domains/EmailNotification/EmailConsumer.cs
using MassTransit;
using System.Text.Json;

namespace Api.Domains.EmailNotification
{
    public class CallSendEmailApiConsumer : IConsumer<CallSendEmailApi>
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CallSendEmailApiConsumer> _logger;

        public CallSendEmailApiConsumer(IHttpClientFactory httpClientFactory, ILogger<CallSendEmailApiConsumer> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<CallSendEmailApi> context)
        {
            var command = context.Message;
            _logger.LogInformation($"📧 Calling Send Email API for correlation ID: {command.CorrelationId}");

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var json = JsonSerializer.Serialize(command.EmailData);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(EmailDomainConstants.ApiEndpoints.SendEmail, content, cts.Token);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    await context.Publish(new SendEmailApiSucceeded
                    {
                        CorrelationId = command.CorrelationId,
                        Response = responseContent
                    });
                    _logger.LogInformation($"✅ Email sent successfully for correlation ID: {command.CorrelationId}");
                }
                else
                {
                    await context.Publish(new SendEmailApiFailed
                    {
                        CorrelationId = command.CorrelationId,
                        Error = $"HTTP {response.StatusCode}: {responseContent}",
                        RetryCount = command.RetryCount + 1
                    });
                    _logger.LogWarning($"❌ Email send failed for correlation ID: {command.CorrelationId}");
                }
            }
            catch (Exception ex)
            {
                await context.Publish(new SendEmailApiFailed
                {
                    CorrelationId = command.CorrelationId,
                    Error = ex.Message,
                    RetryCount = command.RetryCount + 1
                });
                _logger.LogError(ex, $"❌ Email send exception for correlation ID: {command.CorrelationId}");
            }
        }
    }
}
```

## 🎯 Registration in Program.cs
```csharp
// Add to Program.cs
builder.Services.AddSingleton<EmailSendStep>();
x.AddSagaStateMachine<EmailNotificationSaga, EmailNotificationSagaState>()
    .EntityFrameworkRepository(r => { r.ExistingDbContext<MessageDbContext>(); r.ConcurrencyMode = ConcurrencyMode.Optimistic; });
x.AddConsumer<CallSendEmailApiConsumer>();
```

## 🎯 Message Format
```json
{
  "Id": "guid-here",
  "StepData": {
    "welcome-email": {
      "ToEmail": "user@example.com",
      "UserName": "John Doe",
      "TemplateId": "welcome-template"
    }
  }
}
```

## ✅ Benefits for Single-Step Sagas

| **Benefit** | **Single-Step Advantage** |
|-------------|---------------------------|
| **🔄 Retry Logic** | Same 3-retry pattern with exponential backoff |
| **🛡️ Outbox Pattern** | Same guaranteed delivery protection |
| **📊 State Tracking** | Complete audit trail and monitoring |
| **🚀 Recovery** | Survives app restarts automatically |
| **🎯 Simplicity** | Even simpler than multi-step - just one state! |
| **⚡ Performance** | Ultra-fast single API call with full resilience |

## 🎯 Perfect Use Cases for Single-Step Sagas

1. **📧 Email Notifications** - Welcome emails, password resets
2. **📱 Push Notifications** - Mobile app notifications  
3. **📊 Analytics Events** - Send tracking data to analytics service
4. **🔍 Audit Logging** - Send audit events to external logging service
5. **🔔 Webhooks** - Notify external systems of events
6. **💾 Data Sync** - Sync data to external systems
7. **📈 Metrics Collection** - Send metrics to monitoring systems

## 🚀 Why Use Saga for Single Steps?

**You might ask: "Why not just call the API directly?"**

| **Direct API Call** | **Single-Step Saga** |
|--------------------|-----------------------|
| ❌ No retry logic | ✅ 3 automatic retries |
| ❌ Lost on restart | ✅ Survives app restarts |
| ❌ No failure tracking | ✅ Complete audit trail |
| ❌ No monitoring | ✅ Full observability |
| ❌ Fire-and-forget | ✅ Guaranteed delivery |

**Conclusion: The framework makes single-step sagas EASIER than multi-step ones!** 🎉