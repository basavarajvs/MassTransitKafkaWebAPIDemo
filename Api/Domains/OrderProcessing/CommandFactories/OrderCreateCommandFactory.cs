using Api.SagaFramework;

namespace Api.Domains.OrderProcessing.CommandFactories
{
    /// <summary>
    /// Command factory for Order Create operations.
    /// Creates CallOrderCreateApi commands with proper initialization.
    /// </summary>
    public class OrderCreateCommandFactory : ICommandFactory<CallOrderCreateApi, object>
    {
        /// <summary>
        /// Create CallOrderCreateApi command with the specified parameters.
        /// </summary>
        /// <param name="correlationId">Saga correlation ID for tracking</param>
        /// <param name="data">Order data from message StepData["order-created"]</param>
        /// <param name="retryCount">Current retry attempt number</param>
        /// <returns>Fully initialized CallOrderCreateApi command</returns>
        public CallOrderCreateApi Create(Guid correlationId, object data, int retryCount = 0)
                {
            return new CallOrderCreateApi
            {
                CorrelationId = correlationId,
                OrderData = data,
                RetryCount = retryCount
            };
        }
    }
}
