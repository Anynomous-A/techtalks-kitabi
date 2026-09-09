namespace Kitabi.Endpoints
{
    public static class CommunicationEndpoints
    {
        public static void MapCommunicationEndpoints(this WebApplication app)
        {
            // WhatsApp redirect
            app.MapGet("/whatsapp", (
                string phone,
                string? message) =>
            {
                var msg = string.IsNullOrEmpty(message) ? "Hello" : message;
                var url = $"https://wa.me/{phone}?text={Uri.EscapeDataString(msg)}";
                return Results.Redirect(url);
            });
            // WhatsApp link (JSON)
            app.MapGet("/whatsapp/link", (
                string phone,
                string? message) =>
            {
                var msg = string.IsNullOrEmpty(message) ? "Hello" : message;
                var url = $"https://wa.me/{phone}?text={Uri.EscapeDataString(msg)}";
                return Results.Ok(new { whatsappUrl = url });
            });
            // SMS redirect
            app.MapGet("/sms", (
                string phone,
                string? message) =>
            {
                var msg = string.IsNullOrEmpty(message) ? "Hello" : message;
                var url = $"sms:{phone}?&body={Uri.EscapeDataString(msg)}";
                return Results.Redirect(url);
            });
            // SMS link (JSON)
            app.MapGet("/sms/link", (
                string phone,
                string? message) =>
            {
                var msg = string.IsNullOrEmpty(message) ? "Hello" : message;
                var url = $"sms:{phone}?&body={Uri.EscapeDataString(msg)}";
                return Results.Ok(new { smsUrl = url });
            });
        }
    }
}
