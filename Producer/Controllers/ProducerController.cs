
using MassTransit;
using Messages;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Producer.Controllers
{
    /// <summary>
    /// Producer API Controller - provides HTTP endpoint for publishing messages to Kafka.
    /// 
    /// PURPOSE:
    /// - External systems can send messages via HTTP instead of direct Kafka integration
    /// - Simplifies integration for systems that don't have Kafka client libraries
    /// - Provides REST API abstraction over Kafka messaging
    /// - Enables message validation and transformation before Kafka publishing
    /// 
    /// WHY SEPARATE PRODUCER SERVICE:
    /// - Clear separation between message production and consumption
    /// - Can be scaled independently based on load patterns
    /// - Different deployment and monitoring requirements
    /// - Allows for different authentication/authorization policies
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class ProducerController : ControllerBase
    {
        private readonly ITopicProducer<Message> _producer;

        /// <summary>
        /// Constructor with MassTransit Kafka topic producer injection.
        /// 
        /// WHY ITOPICPRODUCER:
        /// - MassTransit abstraction over Kafka producer
        /// - Handles serialization, partitioning, and error handling
        /// - Provides consistent interface regardless of message broker
        /// - Automatic correlation ID and message tracking
        /// </summary>
        public ProducerController(ITopicProducer<Message> producer)
        {
            _producer = producer;
        }

        /// <summary>
        /// HTTP POST endpoint to publish messages to Kafka topic.
        /// 
        /// REQUEST FLOW:
        /// 1. Accept full Message object from HTTP request body
        /// 2. Ensure message has valid ID for tracking
        /// 3. Publish to Kafka topic using MassTransit
        /// 4. Return confirmation with message details
        /// 
        /// WHY ACCEPT FULL MESSAGE OBJECT:
        /// - Maintains compatibility with direct Kafka producers
        /// - Allows complex step data structures
        /// - No need for message transformation in producer
        /// - Consistent message format across all entry points
        /// 
        /// DESIGN DECISION - ID GENERATION:
        /// Generate ID here if not provided, ensuring every message is trackable
        /// This supports both scenarios: client-generated IDs and server-generated IDs
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Message message)
        {
            // Ensure Id is set if not provided - enables message tracking and correlation
            // WHY CHECK GUID.EMPTY: Default Guid value when client doesn't provide ID
            if (message.Id == Guid.Empty)
            {
                // Create new record with generated ID (records are immutable)
                message = message with { Id = Guid.NewGuid() };
            }
            
            // Publish message to Kafka topic - MassTransit handles serialization and routing
            await _producer.Produce(message);
            
            // Return confirmation with message tracking information
            // Includes step count for client visibility into message complexity
            return Ok(new { 
                Message = "Message sent to Kafka", 
                MessageId = message.Id, 
                StepCount = message.StepData.Count 
            });
        }
    }
}
