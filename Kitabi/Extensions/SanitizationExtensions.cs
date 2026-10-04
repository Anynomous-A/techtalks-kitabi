namespace Kitabi.Extensions
{
    public static class SanitizationExtensions
    {
        public static string? Sanitize(this string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            return input.Trim();
        }

        public static string? SanitizeEmail(this string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            return input.Trim().ToLowerInvariant();
        }

        public static string? StripHtml(this string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            return System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", string.Empty).Trim();
        }
    }
}
