using System.Data;
using Microsoft.IdentityModel.Tokens;
using server.src.Exceptions;

namespace server.src.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
        catch (Exception) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Request {Method} {Path} was cancelled by the client.", context.Request.Method, context.Request.Path);
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, exception.Message),
            SecurityTokenException     => (StatusCodes.Status401Unauthorized, exception.Message),
            ForbiddenException         => (StatusCodes.Status403Forbidden,    exception.Message),
            KeyNotFoundException       => (StatusCodes.Status404NotFound,     exception.Message),
            InvalidOperationException  => (StatusCodes.Status400BadRequest,   exception.Message),
            DBConcurrencyException     => (StatusCodes.Status409Conflict,     exception.Message),
            StorageException           => (StatusCodes.Status503ServiceUnavailable, "File storage is unavailable. Please try again later."),
            _                          => (StatusCodes.Status500InternalServerError, "An unexpected internal server error occurred."),
        };

        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(new
        {
            error = true,
            status = statusCode,
            message
        });
    }
}
