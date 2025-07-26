using Messages;
using Api.SagaFramework;

namespace Api.Domains.OrderProcessing.SagaSteps
{
    /// <summary>
    /// Order Processing Step - Domain-specific step for order processing
    /// Uses the generic framework but lives in the Order domain
    /// </summary>
    [SagaStep(StepName = "OrderProcess", MessageKey = OrderDomainConstants.StepKeys.OrderProcessed, MaxRetries = 3, DataPropertyName = "ProcessData")]
    public class OrderProcessStep : GenericStepBase<CallOrderProcessApi, OrderProcessingSagaState>
    {
        public OrderProcessStep(ILogger<OrderProcessStep> logger) 
            : base(logger, GenericStepFactory.Create<OrderProcessStep, OrderProcessingSagaState>(), "ProcessData")
        {
        }

        public override CallOrderProcessApi CreateCommand<TMessage>(Guid correlationId, TMessage message, int retryCount = 0)
        {
            return GenericCommandFactory.Create<CallOrderProcessApi>(correlationId, ExtractStepData(message), _dataPropertyName, retryCount);
        }
    }
} 