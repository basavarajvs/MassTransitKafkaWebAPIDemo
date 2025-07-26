using Messages;
using Api.SagaFramework;

namespace Api.Domains.OrderProcessing.SagaSteps
{
    /// <summary>
    /// Order Shipping Step - Domain-specific step for order shipping
    /// Uses the generic framework but lives in the Order domain
    /// </summary>
    [SagaStep(StepName = "OrderShip", MessageKey = OrderDomainConstants.StepKeys.OrderShipped, MaxRetries = 3, DataPropertyName = "ShipData")]
    public class OrderShipStep : GenericStepBase<CallOrderShipApi, OrderProcessingSagaState>
    {
        public OrderShipStep(ILogger<OrderShipStep> logger) 
            : base(logger, GenericStepFactory.Create<OrderShipStep, OrderProcessingSagaState>(), "ShipData")
        {
        }

        public override CallOrderShipApi CreateCommand<TMessage>(Guid correlationId, TMessage message, int retryCount = 0)
        {
            return GenericCommandFactory.Create<CallOrderShipApi>(correlationId, ExtractStepData(message), _dataPropertyName, retryCount);
        }
    }
} 