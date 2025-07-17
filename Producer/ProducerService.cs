using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Messages;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;

namespace Producer
{
    public class ProducerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ProducerService> _logger;

        public ProducerService(IServiceProvider serviceProvider, ILogger<ProducerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait a bit to ensure the rider is fully started
            await Task.Delay(5000, stoppingToken);

            // Get the rider from the service provider
            var rider = _serviceProvider.GetRequiredService<IKafkaRider>();
            var producer = rider.GetProducer<Null, Message>(new Uri("topic:my-topic"));

            _logger.LogInformation("Starting message production...");

            for (int i = 0; !stoppingToken.IsCancellationRequested; i++)
            {
                var message = new Message { Text = $"Hello, Kafka! Message {i}" };
                try
                {
                    await producer.Produce(null, message, stoppingToken);
                    _logger.LogInformation($"Sent: {message.Text}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error sending message: {message.Text}");
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}