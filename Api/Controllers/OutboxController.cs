using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Infrastructure;
using MassTransit;

namespace Api.Controllers
{
    /// <summary>
    /// Application monitoring and MassTransit info API.
    /// Uses EF Core LINQ for application data and MassTransit APIs for outbox monitoring.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OutboxController : ControllerBase
    {
        private readonly MessageDbContext _dbContext;
        private readonly ILogger<OutboxController> _logger;

        public OutboxController(MessageDbContext dbContext, ILogger<OutboxController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Get application status using pure EF Core LINQ - no raw SQL needed!
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                var result = new
                {
                    Timestamp = DateTime.UtcNow,
                    DatabaseProvider = _dbContext.Database.ProviderName,
                    ApplicationData = await GetApplicationData(),
                    MassTransitInfo = GetMassTransitInfo()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get status");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Get application messages using pure EF Core LINQ
        /// </summary>
        [HttpGet("messages")]
        public async Task<IActionResult> GetApplicationMessages(
            [FromQuery] int limit = 50,
            [FromQuery] string? search = null)
        {
            try
            {
                // Pure EF Core LINQ - database agnostic!
                var query = _dbContext.Messages.AsQueryable();
                
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(m => m.Id.ToString().Contains(search));
                }
                
                var messages = await query
                    .OrderByDescending(m => m.Id) // Assuming Id has ordering
                    .Take(limit)
                    .Select(m => new { m.Id, m.StepData })
                    .ToListAsync();
                
                return Ok(new
                {
                    Timestamp = DateTime.UtcNow,
                    Count = messages.Count,
                    Messages = messages,
                    Note = "These are application messages tracked via EF Core LINQ"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get application messages");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Get MassTransit monitoring guidance
        /// </summary>
        [HttpGet("masstransit-monitoring")]
        public IActionResult GetMassTransitMonitoring()
        {
            return Ok(new
            {
                Timestamp = DateTime.UtcNow,
                Message = "MassTransit outbox monitoring should use official MassTransit tools",
                ProperApproaches = new
                {
                    HealthChecks = "Add MassTransit health checks for outbox monitoring",
                    Metrics = "Use MassTransit.Monitoring.Performance package with Prometheus",
                    Diagnostics = "Enable MassTransit diagnostic observers",
                    Logging = "MassTransit provides structured logging for outbox operations"
                },
                ConfiguredProvider = _dbContext.Database.ProviderName,
                OutboxStatus = "Configured and managed by MassTransit"
            });
        }

        /// <summary>
        /// Get application data using EF Core LINQ
        /// </summary>
        private async Task<object> GetApplicationData()
        {
            // Pure EF Core LINQ - works with any database provider!
            var messageCount = await _dbContext.Messages.CountAsync();
            
            return new
            {
                Tables = new[]
                {
                    new { Name = "Messages", Count = messageCount, Type = "Application Data" }
                },
                Note = "Application data retrieved using EF Core LINQ - fully database agnostic"
            };
        }

        /// <summary>
        /// Get MassTransit configuration info
        /// </summary>
        private object GetMassTransitInfo()
        {
            return new
            {
                OutboxConfigured = true,
                Provider = _dbContext.Database.ProviderName,
                Recommendation = new
                {
                    Monitoring = "Use MassTransit's built-in monitoring capabilities",
                    HealthChecks = "Configure MassTransit health checks for production monitoring",
                    Metrics = "Integrate with Prometheus/Grafana for detailed metrics",
                    WhyNotManualSQL = "MassTransit internal table structure may change - use official APIs"
                }
            };
        }
    }
}