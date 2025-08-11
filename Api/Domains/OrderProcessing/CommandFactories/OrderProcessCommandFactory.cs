using Api.SagaFramework;

namespace Api.Domains.OrderProcessing.CommandFactories
{
    /// <summary>
    /// Command factory for Order Process operations.
    /// Creates CallOrderProcessApi commands with proper initialization.
    /// </summary>
    public class OrderProcessCommandFactory : ICommandFactory<CallOrderProcessApi, object>
    {
        /// <summary>
        /// Create CallOrderProcessApi command with the specified parameters.
        /// </summary>
        /// <param name="correlationId">Saga correlation ID for tracking</param>
        /// <param name="data">Process data from message StepData["order-processed"]</param>
        /// <param name="retryCount">Current retry attempt number</param>
        /// <returns>Fully initialized CallOrderProcessApi command</returns>
        public CallOrderProcessApi Create(Guid correlationId, object data, int retryCount = 0)
        {
            return new CallOrderProcessApi
            {
                CorrelationId = correlationId,
                ProcessData = data,
                RetryCount = retryCount
            };
        }
    }
}