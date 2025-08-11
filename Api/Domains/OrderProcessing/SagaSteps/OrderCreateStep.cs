using Messages;
using Api.SagaFramework;
using Api.Domains.OrderProcessing.CommandFactories;

namespace Api.Domains.OrderProcessing.SagaSteps
{
    /// <summary>
    /// Order Creation Step - Uses Factory Interface Pattern for optimal performance.
    /// 
    /// PERFORMANCE IMPROVEMENTS:
    /// - Direct command creation
    /// - No reflection overhead
    /// - Compile-time safety
    /// - Clear debugging experience
    /// 
    /// ARCHITECTURAL PATTERN:
    /// - Lives in Order domain (domain-specific logic)
    /// - Uses factory framework (high-performance infrastructure)
    /// - Follows Template Method pattern (framework does heavy lifting)
    /// - Single Responsibility: only handles order creation step
    /// 
    /// WHY FACTORY INTERFACE APPROACH:
    /// - Explicit dependencies visible in constructor
    /// - Type-safe command creation
    /// - Easy to test with mocked factories
    /// - Clear separation between command creation and business logic
    /// - Framework handles common patterns (retry, error handling, state management)
    /// </summary>
    public class OrderCreateStep : GenericStepBase<CallOrderCreateApi, object, OrderProcessingSagaState>
    {
        /// <summary>
        /// Constructor using dependency injection with explicit factory.
        /// 
        /// FACTORY INJECTION BENEFITS:
        /// - Type-safe command factory injection
        /// - Clear dependencies visible in constructor
        /// - Easy mocking for unit tests
        /// - No reflection or magic framework behavior
        /// - Logger for observability and debugging
        /// </summary>
        /// <param name="logger">Logger for observability</param>
        /// <param name="commandFactory">Fast, explicit command factory</param>
        public OrderCreateStep(
            ILogger<OrderCreateStep> logger,
            OrderCreateCommandFactory commandFactory) 
            : base(logger, commandFactory, OrderDomainConstants.StepKeys.OrderCreated, maxRetries: 3)
        {
        }

        /// <summary>
        /// Update saga state when step fails.
        /// 
        /// ORDER CREATE FAILURE HANDLING:
        /// - Records retry count for exponential backoff
        /// - Stores error message for debugging
        /// - Updates last error timestamp
        /// - Maintains audit trail
        /// </summary>
        /// <param name="sagaState">Current saga state</param>
        /// <param name="error">Error details</param>
        /// <param name="retryCount">Current retry attempt</param>
        protected override void UpdateSagaStateOnFailure(OrderProcessingSagaState sagaState, string error, int retryCount)
        {
            sagaState.OrderCreateRetryCount = retryCount;
            sagaState.LastError = error;
            sagaState.LastUpdated = DateTime.UtcNow;
        }

        /// <summary>
        /// Update saga state when step succeeds.
        /// 
        /// ORDER CREATE SUCCESS HANDLING:
        /// - Marks API as called
        /// - Stores successful response
        /// - Updates completion timestamp
        /// - Prepares for next step (order processing)
        /// </summary>
        /// <param name="sagaState">Current saga state</param>
        /// <param name="response">Success response from API</param>
        protected override void UpdateSagaStateOnSuccess(OrderProcessingSagaState sagaState, string response)
        {
            sagaState.OrderCreatedApiCalled = true;
            sagaState.OrderCreateResponse = response;
            sagaState.LastUpdated = DateTime.UtcNow;
        }
    }
} 