using HouseholdService.Infrastructure.Data;
using HouseholdService.Infrastructure.EventConsumers.User;
using HouseholdService.Infrastructure.Extensions;
using HouseholdService.Infrastructure.Repositories;
using HouseholdService.Presentation.Endpoints;
using MassTransit;
using MassTransit.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Authorization.Extensions;
using Shared.MassTransit;
using Shared.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DbConnectionOptions>(
    builder.Configuration.GetSection("DbConnectionConfig"));
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMqOptions"));

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

builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

builder.Services.AddScoped<UnitOfWork>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<HouseholdRepository>();
builder.Services.AddScoped<UserHouseholdRepository>();
builder.Services.AddScoped<InviteRepository>();

builder.Services.AddDbContext<HouseholdApiDbContext>((sp, optionsBuilder) => {
    var dbOptions = sp.GetRequiredService<IOptions<DbConnectionOptions>>().Value;

    optionsBuilder.UseNpgsql(dbOptions.ConnectionString, options => {
        options.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    });
});

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<HouseholdApiDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapHouseholdEndpoints();
app.MapInviteEndpoints();

app.MapGet("/", () => "Household API");

app.Run();