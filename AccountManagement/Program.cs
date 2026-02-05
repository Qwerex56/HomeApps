using AccountManagement.Controllers;
using AccountManagement.Data;
using AccountManagement.Extensions;
using AccountManagement.Options;
using Asp.Versioning;
using Asp.Versioning.Conventions;
using Microsoft.EntityFrameworkCore;
using Shared.Authorization.Extensions;
using Shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Config files
builder.Services.Configure<SeedUserOptions>(
    builder.Configuration.GetSection("SeedUser"));
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("JwtOptions"));

// Versioning
builder.Services.AddApiVersioning(options => {
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Version"));
    options.ApiVersionSelector = new CurrentImplementationApiVersionSelector(options);
});

// Auth
builder.Services.AddJwtBearerAuthentication(builder.Configuration);
builder.Services.AddAuthorization(options => { options.AddAllCustomPolicies(); });

// Add controllers to the container.
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add db to the container
builder.Services.AddDbContext<AccountDbContext>(optionsBuilder => {
    optionsBuilder.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddRepositories();
builder.Services.AddUnitOfWorks();
builder.Services.AddAppServices();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options => {
    options.AddPolicy("AllowFrontend", policy => {
        policy.WithOrigins("https://app.localhost")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}

app.UseMiddleware<ExceptionHandlerMiddleware>();

// app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await RegisterBaseOwner.SeedUserAsync(app);

app.Run();