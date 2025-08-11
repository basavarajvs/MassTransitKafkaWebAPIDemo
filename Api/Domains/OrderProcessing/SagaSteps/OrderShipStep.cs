using Microsoft.Extensions.Logging;
using Api.SagaFramework;
using Api.Domains.OrderProcessing.CommandFactories;

namespace Api.Domains.OrderProcessing.SagaSteps
{
    /// <summary>
    /// Order Shipping Step - Handles order shipping workflow step.
    /// </summary>
    public class OrderShipStep : GenericStepBase<CallOrderShipApi, object, OrderProcessingSagaState>
    {
        /// <summary>
        /// Constructor using dependency injection with explicit factory.
        /// </summary>
        public OrderShipStep(
            ILogger<OrderShipStep> logger,
            OrderShipCommandFactory commandFactory) 
            : base(logger, commandFactory, OrderDomainConstants.StepKeys.OrderShipped, maxRetries: 3)
        {
        }

        /// <summary>
        /// Update saga state when step fails.
        /// </summary>
        protected override void UpdateSagaStateOnFailure(OrderProcessingSagaState sagaState, string error, int retryCount)
        {
            sagaState.OrderShipRetryCount = retryCount;
            sagaState.LastError = error;
            sagaState.LastUpdated = DateTime.UtcNow;
        }

        /// <summary>
        /// Update saga state when step succeeds.
        /// </summary>
        protected override void UpdateSagaStateOnSuccess(OrderProcessingSagaState sagaState, string response)
        {
            sagaState.OrderShippedApiCalled = true;
            sagaState.OrderShipResponse = response;
            sagaState.CompletedAt = DateTime.UtcNow;
            sagaState.LastUpdated = DateTime.UtcNow;
        }
    }
}