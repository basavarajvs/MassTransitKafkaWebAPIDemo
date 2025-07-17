using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Messages;
using Consumer;
using Confluent.Kafka;

Console.Title = "Consumer";

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddMassTransit(x =>
        {
            x.UsingInMemory(); // Add a primary in-memory bus

            x.AddRider(rider =>
            {
                rider.AddConsumer<MessageConsumer>(); // Register consumer with the rider

                rider.UsingKafka((context, k) =>
                {
                    k.Host("127.0.0.1:9092");

                    k.TopicEndpoint<Null, Message>("my-topic", "my-new-consumer-group", e =>
                    {
                        e.ConfigureConsumer<MessageConsumer>(context);
                    });
                });
            });
        });
    });

var host = builder.Build();
await host.RunAsync();