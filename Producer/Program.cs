using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Producer;
using Confluent.Kafka;
using Messages;

Console.Title = "Producer";

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddMassTransit(x =>
        {
            // Add a minimal bus configuration
            x.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });

            x.AddRider(rider =>
            {
                rider.AddProducer<Null, Message>("my-topic");
                
                rider.UsingKafka((context, k) =>
                {
                    k.Host("127.0.0.1:9092");
                });
            });
        });
        
        services.AddHostedService<ProducerService>();
    });

var host = builder.Build();
await host.RunAsync();