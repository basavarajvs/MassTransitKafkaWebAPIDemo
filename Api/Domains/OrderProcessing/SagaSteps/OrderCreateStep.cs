using Microsoft.Extensions.Logging;
using Api.SagaFramework;
using Api.Domains.OrderProcessing.CommandFactories;

namespace Api.Domains.OrderProcessing.SagaSteps
{
    /// <summary>
    /// Order Creation Step - Handles order creation workflow step.
    /// </summary>
    public class OrderCreateStep : GenericStepBase<CallOrderCreateApi, object, OrderProcessingSagaState>
    {
        /// <summary>
        /// Constructor using dependency injection with explicit factory.
        /// </summary>
        public OrderCreateStep(
            ILogger<OrderCreateStep> logger,
            OrderCreateCommandFactory commandFactory) 
            : base(logger, commandFactory, OrderDomainConstants.StepKeys.OrderCreated, maxRetries: 3)
        {
        }

        /// <summary>
        /// Update saga state when step fails.
        /// </summary>
        protected override void UpdateSagaStateOnFailure(OrderProcessingSagaState sagaState, string error, int retryCount)
        {
            sagaState.OrderCreateRetryCount = retryCount;
            sagaState.LastError = error;
            sagaState.LastUpdated = DateTime.UtcNow;
        }

        /// <summary>
        /// Update saga state when step succeeds.
        /// </summary>
        protected override void UpdateSagaStateOnSuccess(OrderProcessingSagaState sagaState, string response)
        {
            sagaState.OrderCreatedApiCalled = true;
            sagaState.OrderCreateResponse = response;
            sagaState.LastUpdated = DateTime.UtcNow;
        }
    }
}