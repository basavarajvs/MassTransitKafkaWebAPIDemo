using Api.SagaFramework;

namespace Api.Domains.OrderProcessing.CommandFactories
{
    /// <summary>
    /// Command factory for Order Ship operations.
    /// Creates CallOrderShipApi commands with proper initialization.
    /// </summary>
    public class OrderShipCommandFactory : ICommandFactory<CallOrderShipApi, object>
    {
        /// <summary>
        /// Create CallOrderShipApi command with the specified parameters.
        /// </summary>
        /// <param name="correlationId">Saga correlation ID for tracking</param>
        /// <param name="data">Ship data from message StepData["order-shipped"]</param>
        /// <param name="retryCount">Current retry attempt number</param>
        /// <returns>Fully initialized CallOrderShipApi command</returns>
        public CallOrderShipApi Create(Guid correlationId, object data, int retryCount = 0)
        {
            return new CallOrderShipApi
            {
                CorrelationId = correlationId,
                ShipData = data,
                RetryCount = retryCount
            };
        }
    }
}