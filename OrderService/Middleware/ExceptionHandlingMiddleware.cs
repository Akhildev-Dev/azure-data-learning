using System.Net;
using System.Text.Json;

namespace OrderService.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            HttpRequestException => new
            {
                StatusCode = (int)HttpStatusCode.ServiceUnavailable,
                Message = "External service is unavailable",
                Details = exception.Message
            },
            ArgumentException => new
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Message = "Invalid request",
                Details = exception.Message
            },
            _ => new
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "An error occurred while processing your request",
                Details = exception.Message
            }
        };

        context.Response.StatusCode = response.StatusCode;
        await context.Response.WriteAsJsonAsync(response);
    }
}