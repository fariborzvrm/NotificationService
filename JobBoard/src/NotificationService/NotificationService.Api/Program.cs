using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NotificationService.Infrastructure;
using RabbitMQ.Client;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, loggerConfiguration) =>
        loggerConfiguration.ReadFrom.Configuration(context.Configuration));

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddControllers();

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "JobBoard",
                ValidateAudience = true,
                ValidAudience = "JobBoard",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Auth:Key"] ?? throw new InvalidOperationException("Auth:Key is not configured"))),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });
    builder.Services.AddAuthorization();

    var rabbitMqSection = builder.Configuration.GetSection("RabbitMQ");
    builder.Services.AddHealthChecks()
        .AddSqlServer(
            builder.Configuration.GetConnectionString("DefaultDb")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultDb is not configured"),
            name: "sqlserver")
        .AddRabbitMQ(o =>
            o.ConnectionFactory = new ConnectionFactory
            {
                HostName = rabbitMqSection["Host"] ?? "localhost",
                UserName = rabbitMqSection["Username"] ?? "guest",
                Password = rabbitMqSection["Password"] ?? "guest"
            }, name: "rabbitmq");

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "Notification Service", Version = "v1" }));

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationService.Infrastructure.Persistence.NotificationDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Notification Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
