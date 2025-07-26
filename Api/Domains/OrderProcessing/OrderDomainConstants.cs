namespace Api.Domains.OrderProcessing
{
    /// <summary>
    /// Order Processing Domain Constants - Isolated to Order domain only.
    /// 
    /// WHY SEPARATE CONSTANTS FILE:
    /// - Moved from shared Messages library to maintain domain isolation
    /// - Follows Domain-Driven Design: each domain owns its own constants
    /// - Prevents tight coupling between shared contracts and specific domains
    /// - Easy to add new domains (Payment, Shipping) without touching this file
    /// - Single source of truth for all Order-related string identifiers
    /// </summary>
    public static class OrderDomainConstants
    {
        /// <summary>
        /// Step identifier constants for order creation and shipment process.
        /// 
        /// WHY THESE SPECIFIC KEYS:
        /// - Match the business workflow: Create → Process → Ship
        /// - Past-tense naming reflects completed actions
        /// - Consistent with REST API endpoint naming conventions
        /// - Used by both saga steps and message processing logic
        /// - Human-readable for debugging and monitoring
        /// </summary>
        public static class StepKeys
        {
            /// <summary>Order creation step - initial order placement</summary>
            public const string OrderCreated = "order-created";
            
            /// <summary>Order processing step - payment, inventory, validation</summary>
            public const string OrderProcessed = "order-processed"; 
            
            /// <summary>Order shipping step - final fulfillment</summary>
            public const string OrderShipped = "order-shipped";
        }

        /// <summary>
        /// Order processing workflow configuration constants.
        /// 
        /// WHY CENTRALIZED WORKFLOW CONFIG:
        /// - Single place to adjust retry logic across all order steps
        /// - Easy to tune performance vs reliability tradeoffs
        /// - Consistent behavior across all order processing operations
        /// - Environment-specific values can be externalized later
        /// </summary>
        public static class Workflow
        {
            /// <summary>Saga name for logging and monitoring identification</summary>
            public const string SagaName = "OrderProcessing";
            
            /// <summary>Maximum retries per step before giving up (prevents infinite loops)</summary>
            public const int DefaultMaxRetries = 3;
            
            /// <summary>Timeout per API call in seconds (prevents hanging operations)</summary>
            public const int DefaultTimeoutSeconds = 5;
        }

        /// <summary>
        /// Order API endpoint constants for external service integration.
        /// 
        /// WHY CENTRALIZE ENDPOINTS:
        /// - Single source of truth for external API URLs
        /// - Easy to switch between dev/staging/prod environments
        /// - Prevents typos and inconsistencies across the codebase
        /// - Clear mapping between saga steps and external API calls
        /// </summary>
        public static class ApiEndpoints
        {
            /// <summary>External API for order creation (maps to OrderCreated step)</summary>
            public const string CreateOrder = "/api/orders/create";
            
            /// <summary>External API for order processing (maps to OrderProcessed step)</summary>
            public const string ProcessOrder = "/api/orders/process";
            
            /// <summary>External API for order shipping (maps to OrderShipped step)</summary>
            public const string ShipOrder = "/api/orders/ship";
            
            /// <summary>Base URL for mock external APIs (separate service for clean testing)</summary>
            public const string BaseUrl = "http://localhost:5027";
        }
    }
} 