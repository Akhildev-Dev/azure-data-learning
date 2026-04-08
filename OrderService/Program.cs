using Serilog;
using OrderService.Middleware;
// Configure Serilog BEFORE building the application
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/orderservice-.log", rollingInterval: RollingInterval.Day)
    .Enrich.WithProperty("ServiceName", "OrderService")
    .CreateLogger();

try
{
    Log.Information("Starting OrderService");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info.Title = "Order Service API";
            document.Info.Description = "Microservice for order management with Product Service integration";
            document.Info.Version = "v1.0";
            document.Info.Contact = new()
            {
                Name = "Development Team",
                Email = "dev@orderservice.com",
                Url = new Uri("https://orderservice.com")
            };
            document.Info.License = new()
            {
                Name = "MIT",
                Url = new Uri("https://opensource.org/licenses/MIT")
            };
            return Task.CompletedTask;
        });
    });

    builder.Services.AddHttpClient("ProductService", client =>
    {
        client.BaseAddress = new Uri("https://localhost:7127/");
    });

    var app = builder.Build();
    // Exception handling middleware MUST be first
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseStaticFiles();
        app.MapOpenApi();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "Order API v1");
            options.RoutePrefix = string.Empty;
            options.DocumentTitle = "Order Service - API Documentation";
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            options.DefaultModelsExpandDepth(2);
            options.DefaultModelExpandDepth(1);
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.EnableFilter();
            options.ShowExtensions();
            options.EnableTryItOutByDefault();
            options.DisplayOperationId();
        });
    }

    // Add request logging
    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    Log.Information("OrderService started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "OrderService failed to start");
}
finally
{
    Log.CloseAndFlush();
}