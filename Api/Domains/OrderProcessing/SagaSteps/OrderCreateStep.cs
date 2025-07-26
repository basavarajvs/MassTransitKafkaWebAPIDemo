using Messages;
using Api.SagaFramework;

namespace Api.Domains.OrderProcessing.SagaSteps
{
    /// <summary>
    /// Order Creation Step - handles the first step of order processing workflow.
    /// 
    /// ARCHITECTURAL PATTERN:
    /// - Lives in Order domain (domain-specific logic)
    /// - Uses generic framework (reusable infrastructure)
    /// - Follows Template Method pattern (framework does heavy lifting)
    /// - Single Responsibility: only handles order creation step
    /// 
    /// WHY SEPARATE STEP CLASSES:
    /// - Each step can have different retry logic, timeouts, validation
    /// - Easy to test individual steps in isolation
    /// - Clear separation of concerns between business steps
    /// - Follows Open/Closed Principle: add new steps without changing existing ones
    /// - Framework handles common patterns (retry, error handling, state management)
    /// </summary>
    [SagaStep(
        StepName = "OrderCreate",                                        // Used for property discovery (OrderCreateRetryCount, etc.)
        MessageKey = OrderDomainConstants.StepKeys.OrderCreated,         // Maps to "order-created" in message StepData
        MaxRetries = 3,                                                  // Order creation can retry 3 times before giving up
        DataPropertyName = "OrderData"                                   // Command property to populate with step data
    )]
    public class OrderCreateStep : GenericStepBase<CallOrderCreateApi, OrderProcessingSagaState>
    {
        /// <summary>
        /// Constructor using dependency injection and framework initialization.
        /// 
        /// WHY THIS PATTERN:
        /// - Logger for observability and debugging
        /// - GenericStepFactory automatically discovers saga state properties using reflection
        /// - "OrderData" is the property name on CallOrderCreateApi command to populate
        /// - Base class handles all common step logic (retry, error handling, state updates)
        /// </summary>
        public OrderCreateStep(ILogger<OrderCreateStep> logger) 
            : base(logger, GenericStepFactory.Create<OrderCreateStep, OrderProcessingSagaState>(), "OrderData")
        {
        }

        /// <summary>
        /// Creates the command to execute this step - the only method each step must implement.
        /// 
        /// WHY MINIMAL IMPLEMENTATION:
        /// - Framework handles everything else (retry logic, error handling, state management)
        /// - Step only needs to know how to create its specific command
        /// - GenericCommandFactory uses reflection to populate command properties
        /// - ExtractStepData gets the relevant data from message using MessageKey
        /// 
        /// COMMAND CREATION:
        /// - Extracts "order-created" data from message
        /// - Populates CallOrderCreateApi.OrderData property
        /// - Sets correlation ID for saga tracking
        /// - Includes retry count for exponential backoff
        /// </summary>
        public override CallOrderCreateApi CreateCommand<TMessage>(Guid correlationId, TMessage message, int retryCount = 0)
        {
            return GenericCommandFactory.Create<CallOrderCreateApi>(correlationId, ExtractStepData(message), _dataPropertyName, retryCount);
        }
    }
} 