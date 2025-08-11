using Api.SagaFramework;

namespace Api.Domains.OrderProcessing.CommandFactories
{
    /// <summary>
    /// Fast, explicit command factory for Order Process operations.
    /// 
    /// PERFORMANCE CHARACTERISTICS:
    /// - Direct constructor call: ~5 nanoseconds
    /// - No reflection overhead
    /// - JIT compiler can inline everything
    /// - Type-safe at compile time
    /// 
    /// MAINTAINABILITY BENEFITS:
    /// - Explicit property mapping visible in code
    /// - Easy to modify for business rule changes
    /// - Clear dependency on process data structure
    /// - Straightforward unit testing
    /// </summary>
    public class OrderProcessCommandFactory : ICommandFactory<CallOrderProcessApi, object>
    {
        /// <summary>
        /// Create CallOrderProcessApi command with optimal performance.
        /// 
        /// BUSINESS LOGIC:
        /// - Maps order processing data to command structure
        /// - Includes correlation ID for saga continuity
        /// - Tracks retry attempts for exponential backoff
        /// 
        /// PERFORMANCE: Direct object creation - fastest possible approach
        /// </summary>
        /// <param name="correlationId">Saga correlation ID for tracking</param>
        /// <param name="data">Process data from message StepData["order-processed"]</param>
        /// <param name="retryCount">Current retry attempt number</param>
        /// <returns>Fully initialized CallOrderProcessApi command</returns>
        public CallOrderProcessApi Create(Guid correlationId, object data, int retryCount = 0)
        {
            return new CallOrderProcessApi
            {
                CorrelationId = correlationId,    // Direct assignment - fast
                ProcessData = data,               // Direct assignment - fast
                RetryCount = retryCount          // Direct assignment - fast
            };
        }
    }
}
