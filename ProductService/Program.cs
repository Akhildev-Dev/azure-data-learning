using Serilog;
using ProductService.Middleware;

// Configure Serilog BEFORE building the application
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/productservice-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting ProductService");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddOpenApi();

    var app = builder.Build();
    // Exception handling middleware MUST be first
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "Product API v1");
            options.RoutePrefix = string.Empty;
        });
    }

    // Add request logging
    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    Log.Information("ProductService started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ProductService failed to start");
}
finally
{
    Log.CloseAndFlush();
}