using EcommercePlatform.Application.Features.Listings.Handlers; // For CQRS handlers
using EcommercePlatform.Application.Features.Orders.Handlers;  // For PlaceOrderCommandHandler
using EcommercePlatform.Application.Interfaces.Repositories;
using EcommercePlatform.Application.Interfaces.Services;
using EcommercePlatform.Application.Interfaces.Common; // For IUnitOfWork
using EcommercePlatform.Infrastructure.Caching;
using EcommercePlatform.Infrastructure.CloudStorage;
using EcommercePlatform.Infrastructure.Persistence;
using EcommercePlatform.Infrastructure.Persistence.Repositories;
using EcommercePlatform.Infrastructure.Persistence.Common; // For UnitOfWork implementation
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using Amazon.S3;
using Amazon.Extensions.NETCore.Setup;
using Serilog; // For Serilog
using System; // For DateTime, InvalidOperationException, ArgumentNullException
using Microsoft.AspNetCore.Builder; // For WebApplicationBuilder, WebApplication
using Microsoft.Extensions.Configuration; // For IConfiguration
using Microsoft.Extensions.DependencyInjection; // For IServiceCollection
using Microsoft.Extensions.Hosting; // For IHostEnvironment
using Microsoft.Extensions.Logging; // For ILogger
using Microsoft.AspNetCore.Http; // For Results

// Serilog initial bootstrap logger (before host is built)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting E-commerce API host builder");

    var builder = WebApplication.CreateBuilder(args);

    // Serilog Integration with ASP.NET Core
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("ApplicationName", context.HostingEnvironment.ApplicationName)
        .Enrich.WithEnvironmentName()); // Add other enrichers as needed

    // Configuration
    var configuration = builder.Configuration;

    // Database Context (MongoDB with EF Core)
    var mongoConnectionString = configuration.GetConnectionString("MongoDbConnection");
    var mongoDatabaseName = configuration["MongoDbDatabaseName"];

    if (string.IsNullOrEmpty(mongoConnectionString) || string.IsNullOrEmpty(mongoDatabaseName))
    {
        Log.Fatal("MongoDB connection string or database name is not configured. Application cannot start.");
        throw new InvalidOperationException("MongoDB connection string or database name is not configured.");
    }
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseMongoDB(mongoConnectionString, mongoDatabaseName);
    });

    // Unit of Work and Repositories
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>(); // Register IUnitOfWork
    builder.Services.AddScoped<IListingRepository, EfCoreListingRepository>();
    builder.Services.AddScoped<IUserRepository, EfCoreUserRepository>();
    builder.Services.AddScoped<IOrderRepository, EfCoreOrderRepository>();
    builder.Services.AddScoped<IReviewRepository, EfCoreReviewRepository>();

    // Application Services & CQRS Handlers
    builder.Services.AddScoped<CreateListingCommandHandler>();
    builder.Services.AddScoped<GetListingByIdQueryHandler>();
    builder.Services.AddScoped<PlaceOrderCommandHandler>(); // Register the handler

    // Infrastructure Services
    // Redis Cache
    var redisConnectionString = configuration.GetConnectionString("RedisConnection");
    if (!string.IsNullOrEmpty(redisConnectionString))
    {
        try
        {
            builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
            builder.Services.AddSingleton<ICacheService, RedisCacheService>();
        }
        catch (RedisConnectionException ex)
        {
            Log.Error(ex, "Failed to connect to Redis. Caching will be unavailable.");
            // Optionally register a null cache service as a fallback
        }
    }
    else
    {
        Log.Warning("Redis connection string is not configured. Caching will be unavailable.");
    }

    // AWS S3 / MinIO Cloud Storage
    AWSOptions? awsOptions = configuration.GetAWSOptions();
    if (awsOptions != null)
    {
        if (!string.IsNullOrEmpty(configuration["AWS:ServiceURL"]))
        {
            awsOptions.DefaultClientConfig.ServiceURL = configuration["AWS:ServiceURL"];
            if (bool.TryParse(configuration["AWS:ForcePathStyle"], out bool forcePathStyle))
            {
                awsOptions.DefaultClientConfig.ForcePathStyle = forcePathStyle;
            }
        }
        builder.Services.AddDefaultAWSOptions(awsOptions);
        builder.Services.AddAWSService<IAmazonS3>();
        builder.Services.AddSingleton<ICloudStorageService, S3StorageService>();
    }
    else
    {
        Log.Warning("AWS Options not configured. S3/MinIO Cloud Storage will be unavailable.");
    }

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Version = "v1",
            Title = "E-commerce Platform API",
            Description = "API for managing a second-hand items e-commerce platform."
        });
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowBlazorDevClient", // Name this policy
            policyBuilder => policyBuilder
                .WithOrigins(
                    "http://localhost:5001",  // Default Kestrel HTTP for Blazor WASM client dev server
                    "https://localhost:7001", // Default Kestrel HTTPS for Blazor WASM client dev server
                    // Add other origins if needed, e.g., your production frontend URL
                    configuration.GetValue<string>("AllowedCorsOrigins") ?? "" // Example for config-driven origins
                 )
                .AllowAnyMethod()
                .AllowAnyHeader());
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    app.UseSerilogRequestLogging(); // Place high in the pipeline

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "E-commerce API V1");
        });
        app.UseDeveloperExceptionPage();
    }
    else
    {
        // app.UseMiddleware<GlobalErrorHandlingMiddleware>(); // If you create one
        app.UseExceptionHandler("/Error"); // Built-in basic error handler
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseCors("AllowBlazorDevClient"); // Apply CORS policy
    // app.UseAuthentication(); // Uncomment when auth is added
    app.UseAuthorization();
    app.MapControllers();
    app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

    Log.Information("E-commerce API host starting to run");
    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "E-commerce API host terminated unexpectedly during startup");
}
finally
{
    Log.CloseAndFlush();