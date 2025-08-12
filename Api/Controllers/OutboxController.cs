using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Infrastructure;
using System.Data;

namespace Api.Controllers
{
    /// <summary>
    /// Outbox monitoring and inspection API.
    /// Provides visibility into MassTransit's outbox tables for debugging and monitoring.
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
        /// Get outbox table status and statistics.
        /// Shows table existence, row counts, and recent activity.
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetOutboxStatus()
        {
            try
            {
                var result = new
                {
                    Timestamp = DateTime.UtcNow,
                    Tables = await GetTableInfo(),
                    Statistics = await GetOutboxStatistics()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get outbox status");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Get all outbox messages with optional filtering.
        /// Shows pending, processed, and failed messages.
        /// </summary>
        [HttpGet("messages")]
        public async Task<IActionResult> GetOutboxMessages(
            [FromQuery] bool? delivered = null,
            [FromQuery] int limit = 50,
            [FromQuery] string? correlationId = null)
        {
            try
            {
                var messages = await GetOutboxMessagesFromDatabase(delivered, limit, correlationId);
                
                var result = new
                {
                    Timestamp = DateTime.UtcNow,
                    Filter = new { Delivered = delivered, Limit = limit, CorrelationId = correlationId },
                    Count = messages.Count,
                    Messages = messages
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get outbox messages");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Get pending outbox messages (not yet delivered).
        /// Useful for monitoring delivery delays and failures.
        /// </summary>
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingMessages([FromQuery] int limit = 20)
        {
            try
            {
                var pendingMessages = await GetOutboxMessagesFromDatabase(delivered: false, limit, null);
                
                var result = new
                {
                    Timestamp = DateTime.UtcNow,
                    PendingCount = pendingMessages.Count,
                    Messages = pendingMessages
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get pending outbox messages");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Get raw SQL query results for advanced inspection.
        /// Allows custom queries against outbox tables.
        /// </summary>
        [HttpPost("query")]
        public async Task<IActionResult> ExecuteCustomQuery([FromBody] CustomQueryRequest request)
        {
            try
            {
                // Validate query for safety (basic protection)
                if (string.IsNullOrWhiteSpace(request.Query) || 
                    !request.Query.Trim().ToLowerInvariant().StartsWith("select"))
                {
                    return BadRequest(new { Error = "Only SELECT queries are allowed" });
                }

                var results = await ExecuteRawSqlQuery(request.Query);
                
                return Ok(new
                {
                    Timestamp = DateTime.UtcNow,
                    Query = request.Query,
                    Results = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute custom query: {Query}", request.Query);
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        private async Task<List<TableInfo>> GetTableInfo()
        {
            var tableInfo = new List<TableInfo>();
            
            // Use EF Core's model to get all entity types (tables)
            var entityTypes = _dbContext.Model.GetEntityTypes();
            
            foreach (var entityType in entityTypes)
            {
                var tableName = entityType.GetTableName();
                if (!string.IsNullOrEmpty(tableName))
                {
                    var rowCount = await GetTableRowCount(tableName);
                    tableInfo.Add(new TableInfo
                    {
                        Name = tableName,
                        RowCount = rowCount,
                        IsOutboxTable = tableName.Contains("Outbox", StringComparison.OrdinalIgnoreCase)
                    });
                }
            }
            
            return tableInfo;
        }

        private async Task<int> GetTableRowCount(string tableName)
        {
            try
            {
                // Use EF Core's SqlQuery with FormattableString for safety and database agnosticism
                var result = await _dbContext.Database.SqlQuery<int>($"SELECT COUNT(*) FROM {tableName}").ToListAsync();
                return result.FirstOrDefault();
            }
            catch
            {
                return -1; // Error getting count
            }
        }

        private async Task<OutboxStatistics> GetOutboxStatistics()
        {
            try
            {
                // Try to get statistics from potential outbox tables
                var outboxTables = FindOutboxTables();
                var stats = new OutboxStatistics();

                foreach (var table in outboxTables)
                {
                    try
                    {
                        // Use EF Core's database-agnostic SQL execution
                        var pendingResult = await _dbContext.Database.SqlQuery<int>(
                            $"SELECT COUNT(*) FROM {table} WHERE Delivered IS NULL OR Delivered = 0").ToListAsync();
                        var deliveredResult = await _dbContext.Database.SqlQuery<int>(
                            $"SELECT COUNT(*) FROM {table} WHERE Delivered IS NOT NULL AND Delivered != 0").ToListAsync();
                        
                        stats.PendingMessages += pendingResult.FirstOrDefault();
                        stats.DeliveredMessages += deliveredResult.FirstOrDefault();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get statistics for table {Table}", table);
                    }
                }

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get outbox statistics");
                return new OutboxStatistics { Error = ex.Message };
            }
        }

        private List<string> FindOutboxTables()
        {
            // Use EF Core's model metadata to find outbox tables
            var entityTypes = _dbContext.Model.GetEntityTypes();
            var outboxTables = new List<string>();
            
            foreach (var entityType in entityTypes)
            {
                var tableName = entityType.GetTableName();
                if (!string.IsNullOrEmpty(tableName) && 
                    tableName.Contains("Outbox", StringComparison.OrdinalIgnoreCase))
                {
                    outboxTables.Add(tableName);
                }
            }
            
            return outboxTables;
        }

        private async Task<List<object>> GetOutboxMessagesFromDatabase(bool? delivered, int limit, string? correlationId)
        {
            var outboxTables = FindOutboxTables();
            var allMessages = new List<object>();

            foreach (var table in outboxTables)
            {
                try
                {
                    // Build database-agnostic query using standard SQL
                    var whereClause = "WHERE 1=1";
                    
                    if (delivered.HasValue)
                    {
                        whereClause += delivered.Value 
                            ? " AND (Delivered IS NOT NULL AND Delivered != 0)"
                            : " AND (Delivered IS NULL OR Delivered = 0)";
                    }
                    
                    if (!string.IsNullOrWhiteSpace(correlationId))
                    {
                        whereClause += $" AND (Body LIKE '%{correlationId}%' OR Headers LIKE '%{correlationId}%')";
                    }

                    var query = $@"
                        SELECT * FROM {table}
                        {whereClause}
                        ORDER BY Created DESC";

                    var results = await ExecuteRawSqlQuery(query);
                    allMessages.AddRange(results);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to query outbox table {Table}", table);
                }
            }

            return allMessages.Take(limit).ToList();
        }

        private async Task<List<Dictionary<string, object>>> ExecuteRawSqlQuery(string query)
        {
            using var command = _dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = query;
            
            await _dbContext.Database.OpenConnectionAsync();
            
            using var reader = await command.ExecuteReaderAsync();
            var results = new List<Dictionary<string, object>>();
            
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[reader.GetName(i)] = value ?? "NULL";
                }
                results.Add(row);
            }
            
            return results;
        }

        public class CustomQueryRequest
        {
            public string Query { get; set; } = string.Empty;
        }

        public class TableInfo
        {
            public string Name { get; set; } = string.Empty;
            public int RowCount { get; set; }
            public bool IsOutboxTable { get; set; }
        }

        public class OutboxStatistics
        {
            public int PendingMessages { get; set; }
            public int DeliveredMessages { get; set; }
            public int TotalMessages => PendingMessages + DeliveredMessages;
            public string? Error { get; set; }
        }
    }
}
