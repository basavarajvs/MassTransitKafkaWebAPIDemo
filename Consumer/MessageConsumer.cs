using MassTransit;
using Messages;

namespace Consumer
{
    public class MessageConsumer : IConsumer<Message>
    {
        public Task Consume(ConsumeContext<Message> context)
        {
            Console.WriteLine($"Received: {context.Message.Text}");
            return Task.CompletedTask;
        }
    }
}