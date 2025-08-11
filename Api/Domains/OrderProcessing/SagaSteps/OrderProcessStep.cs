using Microsoft.Extensions.Logging;
using Api.SagaFramework;
using Api.Domains.OrderProcessing.CommandFactories;

namespace Api.Domains.OrderProcessing.SagaSteps
{
    /// <summary>
    /// Order Processing Step - Handles order processing workflow step.
    /// </summary>
    public class OrderProcessStep : GenericStepBase<CallOrderProcessApi, object, OrderProcessingSagaState>
    {
        /// <summary>
        /// Constructor using dependency injection with explicit factory.
        /// </summary>
        public OrderProcessStep(
            ILogger<OrderProcessStep> logger,
            OrderProcessCommandFactory commandFactory) 
            : base(logger, commandFactory, OrderDomainConstants.StepKeys.OrderProcessed, maxRetries: 3)
        {
        }

        /// <summary>
        /// Update saga state when step fails.
        /// </summary>
        protected override void UpdateSagaStateOnFailure(OrderProcessingSagaState sagaState, string error, int retryCount)
        {
            sagaState.OrderProcessRetryCount = retryCount;
            sagaState.LastError = error;
            sagaState.LastUpdated = DateTime.UtcNow;
        }

        /// <summary>
        /// Update saga state when step succeeds.
        /// </summary>
        protected override void UpdateSagaStateOnSuccess(OrderProcessingSagaState sagaState, string response)
        {
            sagaState.OrderProcessedApiCalled = true;
            sagaState.OrderProcessResponse = response;
            sagaState.LastUpdated = DateTime.UtcNow;
        }
    }
}