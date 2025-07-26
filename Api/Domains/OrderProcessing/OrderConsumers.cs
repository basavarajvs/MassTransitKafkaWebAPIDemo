using MassTransit;
using System.Text.Json;

namespace Api.Domains.OrderProcessing
{
    /// <summary>
    /// Order Create API Consumer - Handles calling the mock order creation API
    /// </summary>
    public class CallOrderCreateApiConsumer : IConsumer<CallOrderCreateApi>
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CallOrderCreateApiConsumer> _logger;

        public CallOrderCreateApiConsumer(IHttpClientFactory httpClientFactory, ILogger<CallOrderCreateApiConsumer> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<CallOrderCreateApi> context)
        {
            var command = context.Message;
            _logger.LogInformation($"📦 Calling Order Create API for correlation ID: {command.CorrelationId}, Retry: {command.RetryCount}");

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var json = JsonSerializer.Serialize(command.OrderData);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("http://localhost:5027/api/orders/create", content, cts.Token);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    await context.Publish(new OrderCreateApiSucceeded
                    {
                        CorrelationId = command.CorrelationId,
                        Response = responseContent
                    });
                    _logger.LogInformation($"✅ Order Create API succeeded for correlation ID: {command.CorrelationId}");
                }
                else
                {
                    await context.Publish(new OrderCreateApiFailed
                    {
                        CorrelationId = command.CorrelationId,
                        Error = $"HTTP {response.StatusCode}: {responseContent}",
                        RetryCount = command.RetryCount + 1
                    });
                    _logger.LogWarning($"❌ Order Create API failed for correlation ID: {command.CorrelationId}, Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                await context.Publish(new OrderCreateApiFailed
                {
                    CorrelationId = command.CorrelationId,
                    Error = ex.Message,
                    RetryCount = command.RetryCount + 1
                });
                _logger.LogError(ex, $"💥 Order Create API exception for correlation ID: {command.CorrelationId}");
            }
        }
    }

    /// <summary>
    /// Order Process API Consumer - Handles calling the mock order processing API
    /// </summary>
    public class CallOrderProcessApiConsumer : IConsumer<CallOrderProcessApi>
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CallOrderProcessApiConsumer> _logger;

        public CallOrderProcessApiConsumer(IHttpClientFactory httpClientFactory, ILogger<CallOrderProcessApiConsumer> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<CallOrderProcessApi> context)
        {
            var command = context.Message;
            _logger.LogInformation($"⚙️ Calling Order Process API for correlation ID: {command.CorrelationId}, Retry: {command.RetryCount}");

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var json = JsonSerializer.Serialize(command.ProcessData);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("http://localhost:5027/api/orders/process", content, cts.Token);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    await context.Publish(new OrderProcessApiSucceeded
                    {
                        CorrelationId = command.CorrelationId,
                        Response = responseContent
                    });
                    _logger.LogInformation($"✅ Order Process API succeeded for correlation ID: {command.CorrelationId}");
                }
                else
                {
                    await context.Publish(new OrderProcessApiFailed
                    {
                        CorrelationId = command.CorrelationId,
                        Error = $"HTTP {response.StatusCode}: {responseContent}",
                        RetryCount = command.RetryCount + 1
                    });
                    _logger.LogWarning($"❌ Order Process API failed for correlation ID: {command.CorrelationId}, Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                await context.Publish(new OrderProcessApiFailed
                {
                    CorrelationId = command.CorrelationId,
                    Error = ex.Message,
                    RetryCount = command.RetryCount + 1
                });
                _logger.LogError(ex, $"💥 Order Process API exception for correlation ID: {command.CorrelationId}");
            }
        }
    }

    /// <summary>
    /// Order Ship API Consumer - Handles calling the mock order shipping API
    /// </summary>
    public class CallOrderShipApiConsumer : IConsumer<CallOrderShipApi>
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CallOrderShipApiConsumer> _logger;

        public CallOrderShipApiConsumer(IHttpClientFactory httpClientFactory, ILogger<CallOrderShipApiConsumer> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<CallOrderShipApi> context)
        {
            var command = context.Message;
            _logger.LogInformation($"🚚 Calling Order Ship API for correlation ID: {command.CorrelationId}, Retry: {command.RetryCount}");

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var json = JsonSerializer.Serialize(command.ShipData);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("http://localhost:5027/api/orders/ship", content, cts.Token);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    await context.Publish(new OrderShipApiSucceeded
                    {
                        CorrelationId = command.CorrelationId,
                        Response = responseContent
                    });
                    _logger.LogInformation($"✅ Order Ship API succeeded for correlation ID: {command.CorrelationId}");
                }
                else
                {
                    await context.Publish(new OrderShipApiFailed
                    {
                        CorrelationId = command.CorrelationId,
                        Error = $"HTTP {response.StatusCode}: {responseContent}",
                        RetryCount = command.RetryCount + 1
                    });
                    _logger.LogWarning($"❌ Order Ship API failed for correlation ID: {command.CorrelationId}, Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                await context.Publish(new OrderShipApiFailed
                {
                    CorrelationId = command.CorrelationId,
                    Error = ex.Message,
                    RetryCount = command.RetryCount + 1
                });
                _logger.LogError(ex, $"💥 Order Ship API exception for correlation ID: {command.CorrelationId}");
            }
        }
    }
} 