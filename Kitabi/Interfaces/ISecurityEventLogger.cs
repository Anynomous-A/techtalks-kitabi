namespace Kitabi.Interfaces;

public interface ISecurityEventLogger
{
    void LogFailedLogin(string username, string? ipAddress);
    void LogInvalidToken(string reason, string? ipAddress);
    void LogSuspiciousActivity(string eventType, string details, string? ipAddress);
}