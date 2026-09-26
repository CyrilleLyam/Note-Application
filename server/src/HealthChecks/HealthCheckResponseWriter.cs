using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace server.src.HealthChecks;

public static class HealthCheckResponseWriter
{
    public static Task WriteJson(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            total_duration_ms = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration_ms = Math.Round(entry.Value.Duration.TotalMilliseconds, 2)
            })
        });
    }
}
