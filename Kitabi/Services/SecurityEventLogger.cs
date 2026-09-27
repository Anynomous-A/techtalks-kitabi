using Kitabi.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Kitabi.Services;

// Centralizes every "this looks fishy" event so they all land in one
// place, with one consistent shape, instead of scattered ad-hoc logs.
public class SecurityEventLogger(ILogger<SecurityEventLogger> logger, IMemoryCache cache) : ISecurityEventLogger
{
    private const int FailedLoginThreshold = 5;
    private static readonly TimeSpan TrackingWindow = TimeSpan.FromMinutes(10);

    public void LogFailedLogin(string username, string? ipAddress)
    {
        logger.LogWarning(
            "SECURITY_EVENT: FailedLogin | User={Username} | IP={IpAddress} | Time={Time}",
            username, ipAddress, DateTime.UtcNow);

        TrackAndEscalate($"login:{ipAddress}:{username}", "PossibleBruteForce",
            $"5+ failed login attempts for user '{username}'", ipAddress);
    }

    public void LogInvalidToken(string reason, string? ipAddress)
    {
        logger.LogWarning(
            "SECURITY_EVENT: InvalidToken | Reason={Reason} | IP={IpAddress} | Time={Time}",
            reason, ipAddress, DateTime.UtcNow);

        TrackAndEscalate($"token:{ipAddress}", "RepeatedInvalidTokens",
            $"5+ invalid/expired token attempts ({reason})", ipAddress);
    }

    public void LogSuspiciousActivity(string eventType, string details, string? ipAddress)
    {
        logger.LogWarning(
            "SECURITY_EVENT: {EventType} | Details={Details} | IP={IpAddress} | Time={Time}",
            eventType, details, ipAddress, DateTime.UtcNow);
    }

    // Simple sliding counter per key (IP+username, or IP alone for tokens).
    // Crossing the threshold inside the window gets logged as a distinct,
    // higher-severity event so it's easy to alert on separately from
    // single one-off failures.
    private void TrackAndEscalate(string key, string escalatedEventType, string details, string? ipAddress)
    {
        var count = cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TrackingWindow;
            return 0;
        });

        count++;
        cache.Set(key, count, TrackingWindow);

        if (count == FailedLoginThreshold)
        {
            LogSuspiciousActivity(escalatedEventType, details, ipAddress);
        }
    }
}