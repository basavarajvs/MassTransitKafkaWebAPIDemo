using MassTransit;
using Microsoft.EntityFrameworkCore;
using Messages;
using Api;
using Confluent.Kafka;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<MessageDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMassTransit(x =>
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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
