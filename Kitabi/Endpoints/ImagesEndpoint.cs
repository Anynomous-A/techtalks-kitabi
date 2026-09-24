using System.Security.Claims;
using Kitabi.Data;
using Kitabi.Models;
using Kitabi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kitabi.Endpoints
{
    public static class ImageEndpoints
    {
        // 10 files x 5 MB + multipart overhead
        private const long MaxRequestBytes = 55 * 1024 * 1024;

        public static void MapImageEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/books/{listingId:int}/images");

            group.MapGet("/", async (int listingId, ApplicationDbContext dbContext) =>
            {
                var listing = await dbContext.Listings
                    .AsNoTracking()
                    .Include(l => l.Images)
                    .FirstOrDefaultAsync(l => l.ListingId == listingId && l.Status != "Removed");

                if (listing is null)
                    return Results.NotFound(new { error = "Listing not found" });

                var images = listing.Images.Select(img => new { img.ImageId, img.ImageUrl });
                return Results.Ok(images);
            });

            group.MapPost("/", async (int listingId, IFormFileCollection images,
                                      ApplicationDbContext dbContext, IImageStorage storage, HttpContext http) =>
            {
                var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Results.Unauthorized();

                var listing = await dbContext.Listings
                    .Include(l => l.Images)
                    .FirstOrDefaultAsync(l => l.ListingId == listingId && l.Status != "Removed" && l.SellerId == userId);

                if (listing is null)
                    return Results.NotFound(new { error = "Listing not found" });

                if (images is null || images.Count == 0)
                    return Results.BadRequest(new { error = "No images provided" });

                if (listing.Images.Count + images.Count > storage.MaxFilesPerListing)
                    return Results.BadRequest(new { error = $"Maximum {storage.MaxFilesPerListing} images per listing" });

                //validate everything before writing anything to disk
                var validated = new List<(IFormFile File, string Ext)>();
                foreach (var file in images)
                {
                    var result = await storage.ValidateAsync(file);
                    if (!result.Ok)
                        return Results.BadRequest(new { error = $"{result.Error}" });

                    validated.Add((file, result.Extension!));
                }

                //save and clean up the disk if the database save fails
                var savedUrls = new List<string>();
                try
                {
                    foreach (var (file, ext) in validated)
                    {
                        var url = await storage.SaveAsync(file, ext);
                        savedUrls.Add(url);
                        listing.Images.Add(new ListingImage { ImageUrl = url, Listing = listing });
                    }

                    await dbContext.SaveChangesAsync();
                }
                catch
                {
                    foreach (var url in savedUrls) storage.Delete(url);
                    throw;
                }

                return Results.Ok(new
                {
                    message = $"{savedUrls.Count} images added successfully",
                    added = savedUrls,
                    totalImages = listing.Images.Count
                });
            })
            .WithMetadata(new RequestSizeLimitAttribute(MaxRequestBytes))
            .DisableAntiforgery()
            .RequireAuthorization();

            group.MapDelete("/{imageId:int}", async (int listingId, int imageId,
                                                     ApplicationDbContext dbContext, IImageStorage storage, HttpContext http) =>
            {
                var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Results.Unauthorized();

                var image = await dbContext.ListingImages
                    .FirstOrDefaultAsync(i => i.ListingId == listingId && i.ImageId == imageId && i.Listing.SellerId == userId);

                if (image is null)
                    return Results.NotFound(new { error = "Image not found" });

                storage.Delete(image.ImageUrl);

                dbContext.ListingImages.Remove(image);
                await dbContext.SaveChangesAsync();

                return Results.Ok(new { message = "Image deleted successfully" });
            }).RequireAuthorization();
        }
    }
}