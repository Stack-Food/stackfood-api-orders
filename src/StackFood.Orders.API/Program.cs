using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using StackFood.Orders.Application.Interfaces;
using StackFood.Orders.Application.UseCases;
using StackFood.Orders.Infrastructure.ExternalServices;
using StackFood.Orders.Infrastructure.Persistence;
using StackFood.Orders.Infrastructure.Persistence.Repositories;
using StackFood.Orders.Infrastructure.Consumers;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using System.Diagnostics.CodeAnalysis;

namespace StackFood.Orders.API
{
    [ExcludeFromCodeCoverage]
    public class Program
    {
        private static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            // Swagger Configuration
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "StackFood Orders API",
                    Version = "v1",
                    Description = "API for managing food delivery orders",
                    Contact = new OpenApiContact
                    {
                        Name = "StackFood Team",
                        Email = "team@stackfood.com"
                    }
                });

                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });

            // Database Configuration
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<OrdersDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Repository Registration
            builder.Services.AddScoped<IOrderRepository, OrderRepository>();

            // Use Cases Registration
            builder.Services.AddScoped<CreateOrderUseCase>();
            builder.Services.AddScoped<GetOrderByIdUseCase>();
            builder.Services.AddScoped<GetAllOrdersUseCase>();
            builder.Services.AddScoped<CancelOrderUseCase>();
            builder.Services.AddScoped<UpdateOrderStatusUseCase>();

            // HttpClient for Products Service
            builder.Services.AddHttpClient<IProductService, ProductService>(client =>
            {
                var productsApiUrl = builder.Configuration["ExternalServices:ProductsApiUrl"] ?? "http://localhost:8080";
                client.BaseAddress = new Uri(productsApiUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

            // AWS SNS Configuration
            var useLocalStack = builder.Configuration.GetValue<bool>("AWS:UseLocalStack");
            var awsRegion = builder.Configuration["AWS:Region"] ?? "us-east-1";

            builder.Services.AddSingleton<IAmazonSimpleNotificationService>(sp =>
            {
                if (useLocalStack)
                {
                    var serviceUrl = builder.Configuration["AWS:LocalStack:ServiceUrl"] ?? "http://localhost:4566";
                    var config = new AmazonSimpleNotificationServiceConfig
                    {
                        ServiceURL = serviceUrl,
                        AuthenticationRegion = awsRegion
                    };
                    return new AmazonSimpleNotificationServiceClient("test", "test", config);
                }
                else
                {
                    var config = new AmazonSimpleNotificationServiceConfig
                    {
                        RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(awsRegion)
                    };
                    return new AmazonSimpleNotificationServiceClient(config);
                }
            });

            // Event Publisher Configuration
            builder.Services.AddSingleton<IEventPublisher>(sp =>
            {
                var snsClient = sp.GetRequiredService<IAmazonSimpleNotificationService>();
                var topicArns = new Dictionary<string, string>
                {
                    { "OrderCreated", builder.Configuration["AWS:SNS:OrderCreatedTopicArn"] ?? "arn:aws:sns:us-east-1:000000000000:OrderCreated" },
                    { "OrderCancelled", builder.Configuration["AWS:SNS:OrderCancelledTopicArn"] ?? "arn:aws:sns:us-east-1:000000000000:OrderCancelled" },
                    { "OrderCompleted", builder.Configuration["AWS:SNS:OrderCompletedTopicArn"] ?? "arn:aws:sns:us-east-1:000000000000:OrderCompleted" }
                };
                var logger = sp.GetRequiredService<ILogger<SNSEventPublisher>>();
                return new SNSEventPublisher(snsClient, topicArns, logger);
            });

            // AWS SQS Configuration
            builder.Services.AddSingleton<IAmazonSQS>(sp =>
            {
                if (useLocalStack)
                {
                    var serviceUrl = builder.Configuration["AWS:LocalStack:ServiceUrl"] ?? "http://localhost:4566";
                    var config = new Amazon.SQS.AmazonSQSConfig
                    {
                        ServiceURL = serviceUrl,
                        AuthenticationRegion = awsRegion
                    };
                    return new AmazonSQSClient("test", "test", config);
                }
                else
                {
                    var config = new Amazon.SQS.AmazonSQSConfig
                    {
                        RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(awsRegion)
                    };
                    return new AmazonSQSClient(config);
                }
            });

            // Background Services - Event Consumers
            builder.Services.AddHostedService<PaymentEventsConsumer>();
            builder.Services.AddHostedService<ProductionEventsConsumer>();

            // Health Checks
            builder.Services.AddHealthChecks()
                .AddNpgSql(connectionString!, name: "postgresql", tags: new[] { "db", "sql", "postgresql" });

            // CORS Configuration
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline
            app.UseSwagger();
            app.UseSwaggerUI();


            app.UseHttpsRedirection();
            app.UseCors("AllowAll");
            app.UseAuthorization();
            app.MapControllers();

            // Health Check Endpoints
            app.MapHealthChecks("/health");
            app.MapHealthChecks("/health/ready");

            // Auto-apply migrations and initialize database
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<OrdersDbContext>();
                    await context.Database.MigrateAsync();

                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogInformation("Database migrated successfully");
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while migrating the database");
                }
            }

            app.Run();
        }
    }
}
