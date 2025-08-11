using Api.SagaFramework;

namespace Api.Domains.OrderProcessing.CommandFactories
{
    /// <summary>
    /// Fast, explicit command factory for Order Ship operations.
    /// 
    /// PERFORMANCE CHARACTERISTICS:
    /// - Direct constructor call: ~5 nanoseconds
    /// - No reflection overhead
    /// - JIT compiler can inline everything
    /// - Type-safe at compile time
    /// 
    /// SHIPPING DOMAIN LOGIC:
    /// - Final step in order processing workflow
    /// - Requires successful order creation and processing
    /// - Maps shipping data to command structure
    /// - Maintains audit trail via correlation ID
    /// </summary>
    public class OrderShipCommandFactory : ICommandFactory<CallOrderShipApi, object>
    {
        /// <summary>
        /// Create CallOrderShipApi command with optimal performance.
        /// 
        /// BUSINESS CONTEXT:
        /// - Triggered after successful order processing
        /// - Contains shipping address and logistics data
        /// - Final external API call in order workflow
        /// - Success completes the entire saga
        /// 
        /// PERFORMANCE: Direct object creation - fastest possible approach
        /// </summary>
        /// <param name="correlationId">Saga correlation ID for tracking</param>
        /// <param name="data">Ship data from message StepData["order-shipped"]</param>
        /// <param name="retryCount">Current retry attempt number</param>
        /// <returns>Fully initialized CallOrderShipApi command</returns>
        public CallOrderShipApi Create(Guid correlationId, object data, int retryCount = 0)
        {
            return new CallOrderShipApi
            {
                CorrelationId = correlationId,    // Direct assignment - fast
                ShipData = data,                  // Direct assignment - fast
                RetryCount = retryCount          // Direct assignment - fast
            };
        }
    }
}
