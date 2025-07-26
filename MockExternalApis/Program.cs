var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Mock External APIs",
        Version = "v1",
        Description = "Mock external APIs for testing the MassTransit Saga workflow. Simulates order creation, processing, and shipping services."
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mock External APIs v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Logger.LogInformation("🔧 Mock External APIs service starting...");
app.Logger.LogInformation("📍 Available endpoints:");
app.Logger.LogInformation("   • POST /api/orders/create - Simulate order creation");
app.Logger.LogInformation("   • POST /api/orders/process - Simulate order processing");
app.Logger.LogInformation("   • POST /api/orders/ship - Simulate order shipping");
app.Logger.LogInformation("   • GET /api/orders/health - Health check");
app.Logger.LogInformation("🌐 Swagger UI: http://localhost:5027/swagger");

app.Run();
