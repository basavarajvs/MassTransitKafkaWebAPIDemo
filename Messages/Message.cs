namespace Messages
{
    /// <summary>
    /// Generic message contract - completely domain-agnostic shared library.
    /// 
    /// WHY THIS DESIGN:
    /// - Shared across all microservices (Producer, Api, MockExternalApis)
    /// - No domain-specific knowledge (Order, Payment, Shipping domains are separate)
    /// - Follows Domain-Driven Design principles with clean bounded contexts
    /// - Enables unlimited domain expansion without touching shared contracts
    /// - Kafka serialization/deserialization works seamlessly with this structure
    /// 
    /// ARCHITECTURAL BENEFIT:
    /// Any domain can use this same Message structure - just define their own step keys.
    /// </summary>
    public record Message
    {
        /// <summary>
        /// Primary key for database storage and correlation across services.
        /// 
        /// WHY GUID:
        /// - Unique across distributed systems
        /// - No database round-trip needed for ID generation
        /// - Safe for concurrent operations across multiple instances
        /// - Works with Entity Framework as primary key
        /// </summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>
        /// Generic dictionary to store step data using domain-specific string identifiers as keys.
        /// 
        /// WHY DICTIONARY APPROACH:
        /// - Completely flexible: any domain can define any number of steps
        /// - JSON serialization works seamlessly for Kafka transport
        /// - No coupling to specific business domains (Order, Payment, etc.)
        /// - Enables complex workflows: each key represents a step in a business process
        /// - Value can be any object: simple strings, complex domain objects, arrays, etc.
        /// 
        /// EXAMPLES:
        /// Order Domain: {"order-created": {...}, "order-processed": {...}, "order-shipped": {...}}
        /// Payment Domain: {"payment-authorized": {...}, "payment-captured": {...}}
        /// Shipping Domain: {"package-created": {...}, "package-shipped": {...}}
        /// </summary>
        public Dictionary<string, object> StepData { get; init; } = new();
    }
}