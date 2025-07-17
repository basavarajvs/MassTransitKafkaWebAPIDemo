
using MassTransit;
using Messages;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Api
{
    public class MessageConsumer : IConsumer<Message>
    {
        private readonly MessageDbContext _dbContext;
        private readonly ILogger<MessageConsumer> _logger;

        public MessageConsumer(MessageDbContext dbContext, ILogger<MessageConsumer> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<Message> context)
        {
            _logger.LogInformation($"Received: {context.Message.Text}");
            _dbContext.Messages.Add(context.Message);
            await _dbContext.SaveChangesAsync();
        }
    }
}
