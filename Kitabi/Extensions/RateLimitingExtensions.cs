using System.Threading.RateLimiting;

namespace Kitabi.Extensions;

public static class RateLimitingExtensions
{
    // Tight policy for sensitive, abuse-prone endpoints (login, register, refresh)
    public const string AuthPolicy = "AuthPolicy";

    // Looser, general-purpose policy for the rest of the API
    public const string GlobalPolicy = "GlobalPolicy";

    public static void AddAppRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // 5 attempts per minute per IP - enough for a real user, not for a script
            options.AddPolicy(AuthPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // 100 requests per minute per IP for general endpoints
            options.AddPolicy(GlobalPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Every rejection is itself a signal worth logging/monitoring
            options.OnRejected = async (context, cancellationToken) =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();

                logger.LogWarning(
                    "SECURITY_EVENT: RateLimitExceeded | IP={IpAddress} | Path={Path} | Method={Method}",
                    context.HttpContext.Connection.RemoteIpAddress,
                    context.HttpContext.Request.Path,
                    context.HttpContext.Request.Method);

                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync(
                    "{\"error\":\"Too many requests. Please try again later.\"}",
                    cancellationToken);
            };
        });
    }

    // Partitioning by IP means one abusive client can't starve everyone else's quota
    private static string GetPartitionKey(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}