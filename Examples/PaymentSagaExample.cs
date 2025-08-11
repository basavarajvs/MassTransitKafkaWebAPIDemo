// EXAMPLE: How to use the Factory Interface Pattern framework for Payment Processing
// This demonstrates that our framework is NOT tied to Orders!

using MassTransit;
using Api.SagaFramework;

namespace Examples
{
    // ✅ EXAMPLE 1: Payment Saga State (completely different domain)
    public class PaymentProcessingSagaState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = string.Empty;

        // Payment-specific properties (different naming pattern)
        public int AuthorizeRetryCount { get; set; }
        public int CaptureRetryCount { get; set; }
        public int RefundRetryCount { get; set; }

        public bool AuthorizeCompleted { get; set; }
        public bool CaptureCompleted { get; set; }
        public bool RefundCompleted { get; set; }

        public string? AuthorizeResponse { get; set; }
        public string? CaptureResponse { get; set; }
        public string? RefundResponse { get; set; }

        public DateTime? StartedAt { get; set; }
        public DateTime? LastUpdated { get; set; }
    }

    // ✅ EXAMPLE 2: Payment Commands (completely different from Order commands)
    public record AuthorizePaymentCommand
    {
        public Guid CorrelationId { get; init; }
        public object PaymentData { get; init; } = null!;
        public int RetryCount { get; init; }
    }

    public record CapturePaymentCommand
    {
        public Guid CorrelationId { get; init; }
        public object PaymentData { get; init; } = null!;
        public int RetryCount { get; init; }
    }

    // ✅ EXAMPLE 3: Command Factories using Factory Interface Pattern
    public class AuthorizePaymentCommandFactory : ICommandFactory<AuthorizePaymentCommand, object>
    {
        public AuthorizePaymentCommand Create(Guid correlationId, object data, int retryCount = 0)
        {
            return new AuthorizePaymentCommand
            {
                CorrelationId = correlationId,
                PaymentData = data,
                RetryCount = retryCount
            };
        }
    }

    public class CapturePaymentCommandFactory : ICommandFactory<CapturePaymentCommand, object>
    {
        public CapturePaymentCommand Create(Guid correlationId, object data, int retryCount = 0)
        {
            return new CapturePaymentCommand
            {
                CorrelationId = correlationId,
                PaymentData = data,
                RetryCount = retryCount
            };
        }
    }

    // ✅ EXAMPLE 4: Payment Steps using Factory Interface Pattern!
    public class AuthorizePaymentStep : GenericStepBase<AuthorizePaymentCommand, object, PaymentProcessingSagaState>
    {
        public AuthorizePaymentStep(
            ILogger<AuthorizePaymentStep> logger,
            AuthorizePaymentCommandFactory commandFactory) 
            : base(logger, commandFactory, "payment-authorize", maxRetries: 5)
        {
        }

        protected override void UpdateSagaStateOnFailure(PaymentProcessingSagaState sagaState, string error, int retryCount)
        {
            sagaState.AuthorizeRetryCount = retryCount;
            sagaState.LastUpdated = DateTime.UtcNow;
        }

        protected override void UpdateSagaStateOnSuccess(PaymentProcessingSagaState sagaState, string response)
        {
            sagaState.AuthorizeCompleted = true;
            sagaState.AuthorizeResponse = response;
            sagaState.LastUpdated = DateTime.UtcNow;
        }
    }

    public class CapturePaymentStep : GenericStepBase<CapturePaymentCommand, object, PaymentProcessingSagaState>
    {
        public CapturePaymentStep(
            ILogger<CapturePaymentStep> logger,
            CapturePaymentCommandFactory commandFactory) 
            : base(logger, commandFactory, "payment-capture", maxRetries: 3)
        {
        }

        protected override void UpdateSagaStateOnFailure(PaymentProcessingSagaState sagaState, string error, int retryCount)
        {
            sagaState.CaptureRetryCount = retryCount;
            sagaState.LastUpdated = DateTime.UtcNow;
        }

        protected override void UpdateSagaStateOnSuccess(PaymentProcessingSagaState sagaState, string response)
        {
            sagaState.CaptureCompleted = true;
            sagaState.CaptureResponse = response;
            sagaState.LastUpdated = DateTime.UtcNow;
        }
    }

    // ✅ EXAMPLE 5: Shipping Saga (yet another domain)
    public class ShippingSagaState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = string.Empty;

        // Shipping-specific properties (different naming again)
        public int PackageRetries { get; set; }
        public int DeliveryRetries { get; set; }

        public bool PackageCompleted { get; set; }
        public bool DeliveryCompleted { get; set; }

        public string? PackageResult { get; set; }
        public string? DeliveryResult { get; set; }
    }

    public record PackageShipmentCommand
    {
        public Guid CorrelationId { get; init; }
        public object ShippingData { get; init; } = null!;
        public int RetryCount { get; init; }
    }

    public class PackageShipmentCommandFactory : ICommandFactory<PackageShipmentCommand, object>
    {
        public PackageShipmentCommand Create(Guid correlationId, object data, int retryCount = 0)
        {
            return new PackageShipmentCommand
            {
                CorrelationId = correlationId,
                ShippingData = data,
                RetryCount = retryCount
            };
        }
    }

    public class PackageShipmentStep : GenericStepBase<PackageShipmentCommand, object, ShippingSagaState>
    {
        public PackageShipmentStep(
            ILogger<PackageShipmentStep> logger,
            PackageShipmentCommandFactory commandFactory) 
            : base(logger, commandFactory, "ship-package", maxRetries: 2)
        {
        }

        protected override void UpdateSagaStateOnFailure(ShippingSagaState sagaState, string error, int retryCount)
        {
            sagaState.PackageRetries = retryCount;
        }

        protected override void UpdateSagaStateOnSuccess(ShippingSagaState sagaState, string response)
        {
            sagaState.PackageCompleted = true;
            sagaState.PackageResult = response;
        }
    }
}

/*
🎉 WHAT THIS PROVES:

✅ COMPLETE DOMAIN DECOUPLING:
   - PaymentProcessingSaga ≠ OrderProcessingSaga
   - Different state properties, different naming conventions
   - Different command types, different data properties
   - SAME framework works for ALL domains!

✅ FACTORY INTERFACE PATTERN BENEFITS:
   - Type-safe command creation at compile time
   - Explicit dependencies visible in constructors
   - Easy mocking for unit tests
   - No magic attributes or reflection
   - Clear, readable command factory implementations

✅ REUSABLE ARCHITECTURE:
   - Generic SagaFramework (ICommandFactory, GenericStepBase)
   - ~30 lines per step for ANY domain (including factory)
   - Zero repetition across domains
   - Perfect abstraction achieved!

🚀 ADDING NEW DOMAINS IS SIMPLE:
   Just create:
   1. YourSagaState (your properties)
   2. YourCommand records (your data)  
   3. YourCommandFactory (implements ICommandFactory)
   4. YourStep classes (~30 lines each)
   5. Register factory in DI container
   
   The framework handles EVERYTHING else!

🔧 DEPENDENCY INJECTION SETUP:
   builder.Services.AddScoped<AuthorizePaymentCommandFactory>();
   builder.Services.AddScoped<CapturePaymentCommandFactory>();
   builder.Services.AddScoped<PackageShipmentCommandFactory>();
   
   // Sagas inject factories directly for type-safe command creation
*/ 