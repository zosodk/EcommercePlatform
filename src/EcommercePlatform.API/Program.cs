using EcommercePlatform.Application.Features.Listings.Handlers;
        using EcommercePlatform.Application.Features.Orders.Handlers; // For PlaceOrderCommandHandler
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
        using Serilog; 
        using System;
        using Microsoft.AspNetCore.Builder;
        using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Hosting;
        using Microsoft.Extensions.Logging;
        using Microsoft.AspNetCore.Http;

        // Initial Serilog Bootstrap Logger (catches startup errors)
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting E-commerce API host builder...");

            var builder = WebApplication.CreateBuilder(args);

            // --- Serilog Integration with ASP.NET Core ---
            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration) // Reads from appsettings.json "Serilog" section
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ApplicationName", context.HostingEnvironment.ApplicationName)
                .Enrich.WithEnvironmentName());

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
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IListingRepository, EfCoreListingRepository>();
            builder.Services.AddScoped<IUserRepository, EfCoreUserRepository>(); // Assuming EfCoreUserRepository exists
            builder.Services.AddScoped<IOrderRepository, EfCoreOrderRepository>(); // Assuming EfCoreOrderRepository exists
            builder.Services.AddScoped<IReviewRepository, EfCoreReviewRepository>(); // Assuming EfCoreReviewRepository exists

            // Application Services & CQRS Handlers
            builder.Services.AddScoped<CreateListingCommandHandler>();
            builder.Services.AddScoped<GetListingByIdQueryHandler>();
            builder.Services.AddScoped<PlaceOrderCommandHandler>();
            // Add other handlers as they are created

            // Infrastructure Services
            // Redis Cache
            var redisConnectionString = configuration.GetConnectionString("RedisConnection");
            if (!string.IsNullOrEmpty(redisConnectionString))
            {
                try
                {
                    // Ensure Redis is configured to be a singleton for IConnectionMultiplexer
                    builder.Services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(redisConnectionString));
                    builder.Services.AddSingleton<ICacheService, RedisCacheService>();
                    Log.Information("Redis Cache Service configured.");
                }
                catch (RedisConnectionException ex)
                {
                    Log.Error(ex, "Failed to connect to Redis. Caching will be unavailable.");
                }
            }
            else
            {
                Log.Warning("Redis connection string is not configured. Caching will be unavailable.");
            }

            // AWS S3 / MinIO Cloud Storage
            AWSOptions? awsOptions = configuration.GetAWSOptions(); // Reads from "AWS" section in appsettings
            if (awsOptions != null)
            {
                // Override ServiceURL and ForcePathStyle from config if present (for MinIO)
                var awsSection = configuration.GetSection("AWS");
                if (!string.IsNullOrEmpty(awsSection["ServiceURL"]))
                {
                    awsOptions.DefaultClientConfig.ServiceURL = awsSection["ServiceURL"];
                }
                if (bool.TryParse(awsSection["ForcePathStyle"], out bool forcePathStyle))
                {
                    awsOptions.DefaultClientConfig.ForcePathStyle = forcePathStyle;
                }

                builder.Services.AddDefaultAWSOptions(awsOptions);
                builder.Services.AddAWSService<IAmazonS3>(); // Injects IAmazonS3
                builder.Services.AddSingleton<ICloudStorageService, S3StorageService>();
                Log.Information("S3/MinIO Cloud Storage Service configured.");
            }
            else
            {
                Log.Warning("AWS Options not configured in appsettings.json. S3/MinIO Cloud Storage will be unavailable.");
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
                options.AddPolicy("AllowBlazorDevClient",
                    policyBuilder => policyBuilder
                        .WithOrigins(
                            configuration.GetValue<string>("AllowedCorsOrigins:BlazorWasmHttp") ?? "http://localhost:5001",
                            configuration.GetValue<string>("AllowedCorsOrigins:BlazorWasmHttps") ?? "https://localhost:7001"
                         ) // Read from config or use defaults
                        .AllowAnyMethod()
                        .AllowAnyHeader());
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseSerilogRequestLogging(); // Place high in the pipeline for comprehensive request logging

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
                app.UseExceptionHandler("/Error"); // Basic error handler
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors("AllowBlazorDevClient"); // Apply CORS policy
            // app.UseAuthentication(); // Add when authentication is implemented
            app.UseAuthorization();
            app.MapControllers();
            app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

            Log.Information("E-commerce API host is built and configured. Starting run...");
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "E-commerce API host terminated unexpectedly during startup configuration.");
            throw; // Re-throw to ensure process termination if startup fails critically
        }
        finally
        {
            Log.CloseAndFlush(); // Ensure all logs are flushed when application exits
        }