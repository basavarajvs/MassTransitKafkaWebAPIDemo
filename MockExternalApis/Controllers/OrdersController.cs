using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MockExternalApis.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly ILogger<OrdersController> _logger;
        private static readonly Random _random = new Random();

        public OrdersController(ILogger<OrdersController> logger)
        {
            _logger = logger;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] JsonElement orderData)
        {
            try
            {
                // Simulate processing time
                await Task.Delay(_random.Next(100, 500));

                // Simulate occasional failures (10% chance)
                if (_random.Next(1, 101) <= 10)
                {
                    _logger.LogWarning("Order Create API: Simulated failure");
                    return StatusCode(500, new { error = "Simulated order creation failure", timestamp = DateTime.UtcNow });
                }

                _logger.LogInformation($"Order Create API: Successfully processed order data: {orderData}");

                var response = new
                {
                    success = true,
                    message = "Order created successfully",
                    orderId = Guid.NewGuid().ToString(),
                    timestamp = DateTime.UtcNow,
                    processedData = orderData,
                    service = "MockExternalApis"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Order Create API");
                return StatusCode(500, new { error = ex.Message, timestamp = DateTime.UtcNow });
            }
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessOrder([FromBody] JsonElement processData)
        {
            try
            {
                // Simulate processing time
                await Task.Delay(_random.Next(200, 800));

                // Simulate occasional failures (10% chance)
                if (_random.Next(1, 101) <= 10)
                {
                    _logger.LogWarning("Order Process API: Simulated failure");
                    return StatusCode(500, new { error = "Simulated order processing failure", timestamp = DateTime.UtcNow });
                }

                _logger.LogInformation($"Order Process API: Successfully processed order data: {processData}");

                var response = new
                {
                    success = true,
                    message = "Order processed successfully",
                    processId = Guid.NewGuid().ToString(),
                    timestamp = DateTime.UtcNow,
                    processedData = processData,
                    service = "MockExternalApis"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Order Process API");
                return StatusCode(500, new { error = ex.Message, timestamp = DateTime.UtcNow });
            }
        }

        [HttpPost("ship")]
        public async Task<IActionResult> ShipOrder([FromBody] JsonElement shipData)
        {
            try
            {
                // Simulate processing time
                await Task.Delay(_random.Next(300, 1000));

                // Simulate occasional failures (10% chance)
                if (_random.Next(1, 101) <= 10)
                {
                    _logger.LogWarning("Order Ship API: Simulated failure");
                    return StatusCode(500, new { error = "Simulated order shipping failure", timestamp = DateTime.UtcNow });
                }

                _logger.LogInformation($"Order Ship API: Successfully processed order data: {shipData}");

                var response = new
                {
                    success = true,
                    message = "Order shipped successfully",
                    shipmentId = Guid.NewGuid().ToString(),
                    trackingNumber = $"TRK{_random.Next(100000, 999999)}",
                    timestamp = DateTime.UtcNow,
                    processedData = shipData,
                    service = "MockExternalApis"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Order Ship API");
                return StatusCode(500, new { error = ex.Message, timestamp = DateTime.UtcNow });
            }
        }

        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new { 
                status = "healthy", 
                timestamp = DateTime.UtcNow, 
                apis = new[] { "create", "process", "ship" },
                service = "MockExternalApis - External API Simulator"
            });
        }
    }
} 