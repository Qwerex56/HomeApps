using Household.Api.EventConsumers.User;
using MassTransit;
using MassTransit.Configuration;
using Shared.Authorization.Extensions;
using Shared.MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Auth
builder.Services.AddJwtBearerAuthentication(builder.Configuration);
builder.Services.AddAuthorization(options => { options.AddAllCustomPolicies(); });

// Add MassTransit
// Register consumers
builder.Services.AddMassTransit(config => {
    config.RegisterConsumer<UserCreatedConsumer>();
});
// Register Rabbit MQ
builder.Services.AddMassTransitWithRabbitMq(builder.Configuration);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

app.Run();