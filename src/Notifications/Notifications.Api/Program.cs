using HomeApps.BuildingBlocks.Api;
using HomeApps.BuildingBlocks.Infrastructure.Persistence;
using Notifications.Api;
using Notifications.Application;
using Notifications.Infrastructure;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Push;

var builder = WebApplication.CreateBuilder(args);

builder.AddHomeAppsDefaults(typeof(Program).Assembly);
builder.Services.AddNotificationsApi();
builder.Services.AddNotificationsApplication();
builder.Services.AddNotificationsInfrastructure(builder.Configuration);

var app = builder.Build();

// Fails the start in Production when the VAPID keys are missing, instead of on the first notification.
_ = app.Services.GetRequiredService<VapidKeys>();

app.UseHomeAppsDefaults();

await app.MigrateDatabaseOnStartupAsync<NotificationsDbContext>();

await app.RunAsync();
