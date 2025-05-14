using EcommercePlatform.Application.Features.Listings.Handlers;
        using EcommercePlatform.Application.Features.Orders.Handlers;
        using EcommercePlatform.Application.Interfaces.Repositories;
        using EcommercePlatform.Application.Interfaces.Services;
        using EcommercePlatform.Application.Interfaces.Common;
        using EcommercePlatform.Infrastructure.Caching;
        using EcommercePlatform.Infrastructure.CloudStorage;
        using EcommercePlatform.Infrastructure.Persistence;
        using EcommercePlatform.Infrastructure.Persistence.Repositories;
        using EcommercePlatform.Infrastructure.Persistence.Common;
        using Microsoft.EntityFrameworkCore;
        using Microsoft.OpenApi.Models;
        using StackExchange.Redis;
        using Amazon.S3; // For IAmazonS3
        using Amazon.Extensions.NETCore.Setup; // For AWSOptions and AddAWSService
        using Serilog; // Root Serilog namespace
        
        using System;
        using Microsoft.AspNetCore.Builder;
        using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Hosting;
        using Microsoft.Extensions.Logging;
        using Microsoft.AspNetCore.Http;

        // Initial Serilog Bootstrap Logger (catches startup errors before host is fully built)
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
            .Enrich.FromLogContext() // Essential for enriching with context properties
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting E-commerce API host builder...");

            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration) // Reads from appsettings.json "Serilog" section
                .ReadFrom.Services(services) // Allows services to be injected into Serilog (e.g., for custom sinks)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ApplicationName", context.HostingEnvironment.ApplicationName)
                .Enrich.WithEnvironmentName() // From Serilog.Enrichers.Environment
                .Enrich.WithMachineName());   // From Serilog.Enrichers.Environment
                                              // Add other global enrichers here if needed

            // Configuration object
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
            builder.Services.AddScoped<IUserRepository, EfCoreUserRepository>();
            builder.Services.AddScoped<IOrderRepository, EfCoreOrderRepository>();
            builder.Services.AddScoped<IReviewRepository, EfCoreReviewRepository>();

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
                var awsSection = configuration.GetSection("AWS");
                if (!string.IsNullOrEmpty(awsSection["ServiceURL"]))
                {
                    // DefaultClientConfig is available on AWSOptions
                    awsOptions.DefaultClientConfig.ServiceURL = awsSection["ServiceURL"];
                }
                // ForcePathStyle is a property of Amazon.Runtime.ClientConfig, which DefaultClientConfig is.
                if (bool.TryParse(awsSection["ForcePathStyle"], out bool forcePathStyleValue))
                {
                    //awsOptions.DefaultClientConfig.ForcePathStyle = forcePathStyleValue;
                    //Commented out at the moment - gives an error
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
                         )
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
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors("AllowBlazorDevClient");
            // app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

            Log.Information("E-commerce API host is built and configured. Starting run...");
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "E-commerce API host terminated unexpectedly during startup configuration.");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }