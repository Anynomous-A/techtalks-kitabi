using Kitabi.Data;
using Microsoft.EntityFrameworkCore;

namespace Kitabi.Endpoints
{
    public static class CommunicationEndpoints
    {
        public static void MapCommunicationEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/books/{listingId:int}/contact")
                .RequireAuthorization();

            // WhatsApp redirect
            group.MapGet("/whatsapp", async (
                int listingId,
                string? message,
                ApplicationDbContext dbContext) =>
            {
                var phone = await dbContext.Listings
                    .AsNoTracking()
                    .Where(l => l.ListingId == listingId && l.Status != "Removed")
                    .Select(l => l.Seller.Phone)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(phone))
                    return Results.NotFound(new { error = "Listing not found" });

                var msg = string.IsNullOrEmpty(message) ? "Hello" : message;
                var url = $"https://wa.me/{phone}?text={Uri.EscapeDataString(msg)}";
                return Results.Redirect(url);
            });

            // WhatsApp link (JSON)
            group.MapGet("/whatsapp/link", async (
                int listingId,
                string? message,
                ApplicationDbContext dbContext) =>
            {
                var phone = await dbContext.Listings
                    .AsNoTracking()
                    .Where(l => l.ListingId == listingId && l.Status != "Removed")
                    .Select(l => l.Seller.Phone)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(phone))
                    return Results.NotFound(new { error = "Listing not found" });

                var msg = string.IsNullOrEmpty(message) ? "Hello" : message;
                var url = $"https://wa.me/{phone}?text={Uri.EscapeDataString(msg)}";
                return Results.Ok(new { whatsappUrl = url });
            });

            // SMS redirect
            group.MapGet("/sms", async (
                int listingId,
                string? message,
                ApplicationDbContext dbContext) =>
            {
                var phone = await dbContext.Listings
                    .AsNoTracking()
                    .Where(l => l.ListingId == listingId && l.Status != "Removed")
                    .Select(l => l.Seller.Phone)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(phone))
                    return Results.NotFound(new { error = "Listing not found" });

                var msg = string.IsNullOrEmpty(message) ? "Hello" : message;
                var url = $"sms:{phone}?&body={Uri.EscapeDataString(msg)}";
                return Results.Redirect(url);
            });

            // SMS link (JSON)
            group.MapGet("/sms/link", async (
                int listingId,
                string? message,
                ApplicationDbContext dbContext) =>
            {
                var phone = await dbContext.Listings
                    .AsNoTracking()
                    .Where(l => l.ListingId == listingId && l.Status != "Removed")
                    .Select(l => l.Seller.Phone)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(phone))
                    return Results.NotFound(new { error = "Listing not found" });

                var msg = string.IsNullOrEmpty(message) ? "Hello" : message;
                var url = $"sms:{phone}?&body={Uri.EscapeDataString(msg)}";
                return Results.Ok(new { smsUrl = url });
            });
        }
    }
}