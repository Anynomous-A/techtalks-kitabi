using Kitabi.Data;
using Kitabi.Models;
using Microsoft.EntityFrameworkCore;

namespace Kitabi.Endpoints
{
    public static class ImageEndpoints
    {
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

                var images = listing.Images.Select(img => new
                {
                    img.ImageId,
                    img.ImageUrl
                });

                return Results.Ok(images);
            });

          group.MapPost("/", async (int listingId,IFormFileCollection images, ApplicationDbContext dbContext, IWebHostEnvironment env) =>
            {
                
                var listing = await dbContext.Listings
                    .Include(l => l.Images)
                    .FirstOrDefaultAsync(l => l.ListingId == listingId && l.Status != "Removed");

                if (listing is null)
                    return Results.NotFound(new { error = "Listing not found" });

                if (images is null || images.Count == 0)
                    return Results.BadRequest(new { error = "No images provided" });

                if (listing.Images.Count + images.Count > 10)
                    return Results.BadRequest(new { error = "Maximum 10 images per listing" });
        
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg", "image/gif" };

                foreach (var file in images)
                {
                    if (!allowedTypes.Contains(file.ContentType.ToLower()))
                        return Results.BadRequest(new { error = $"Invalid file type: {file.FileName}. Only JPEG, PNG, and GIF allowed." });

                    if (file.Length > 5 * 1024 * 1024)
                        return Results.BadRequest(new { error = $"File too large: {file.FileName}. Maximum 5MB limit." });
                }

                var uploadDir = Path.Combine(env.WebRootPath ?? "wwwroot", "uploads");
                Directory.CreateDirectory(uploadDir);

                var addedImages = new List<string>();

                foreach (var file in images)
                {
                    var safeFileName = Path.GetFileName(file.FileName);
                    var fileName = $"{Guid.NewGuid()}_{safeFileName}";
                    var path = Path.Combine(uploadDir, fileName);

                  
                    using (var stream = File.Create(path))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var image = new ListingImage
                    {
                        ImageUrl = $"/uploads/{fileName}",
                        Listing = listing
                    };

                    listing.Images.Add(image);
                    addedImages.Add(fileName);
                }

                await dbContext.SaveChangesAsync();

                return Results.Ok(new
                {
                    message = $"{addedImages.Count} images added successfully",
                    added = addedImages,
                    totalImages = listing.Images.Count
                });
            })
            .DisableAntiforgery();

            group.MapDelete("/{imageId:int}", async (int listingId, int imageId, ApplicationDbContext dbContext) =>
            {
                var image = await dbContext.ListingImages
                    .FirstOrDefaultAsync(i => i.ListingId == listingId && i.ImageId == imageId);

                if (image is null)
                    return Results.NotFound(new { error = "Image not found" });


                var filePath = Path.Combine("wwwroot", image.ImageUrl.TrimStart('/'));
                if (File.Exists(filePath))
                    File.Delete(filePath);

                dbContext.ListingImages.Remove(image);
                await dbContext.SaveChangesAsync();

                return Results.Ok(new { message = "Image deleted successfully" });
            });
        }
    }
}