using Serilog.Context;

namespace server.src.Middlewares;

public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    private const int MaxLength = 64;

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context.Request.Headers[HeaderName].ToString());

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(string incoming)
    {
        var isValid = incoming.Length is > 0 and <= MaxLength
            && incoming.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');

        return isValid ? incoming : Guid.NewGuid().ToString("N");
    }
}
