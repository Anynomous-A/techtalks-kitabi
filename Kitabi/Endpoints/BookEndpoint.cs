using System.Security.Claims;
using Kitabi.Data;
using Kitabi.Dtos.Book;
using Kitabi.Filters;
using Kitabi.Models;
using Kitabi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kitabi.Endpoints
{
    public static class BookEndpoints
    {
        // 10 files x 5 MB + multipart overhead 
        private const long MaxRequestBytes = 55 * 1024 * 1024;

        public static void MapBookEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/books");
            group.MapGet("/", async (ApplicationDbContext dbContext) =>
            {
                var listings = await dbContext.Listings
                    .AsNoTracking()
                    .Include(l => l.Book)
                    .Include(l => l.Images)
                    .Include(l => l.Seller)
                    .Where(l => l.Status != "Removed")
                    .Select(listing => new ReadBookDto(
                        listing.ListingId,
                        listing.Book.Title,
                        listing.Book.Author,
                        listing.Book.Category,
                        listing.Book.ISBN,
                        listing.Book.Description,
                        listing.Price,
                        listing.Condition,
                        listing.City,
                        listing.Status,
                        listing.Images.Select(img => img.ImageUrl).ToList(),
                        listing.Seller.Name,
                        listing.Seller.University
                    ))
                    .ToListAsync();

                return Results.Ok(listings);
            });


            group.MapGet("/{id}", async (int id, ApplicationDbContext dbContext) =>
            {
                var listing = await dbContext.Listings
                    .AsNoTracking()
                    .Include(l => l.Book)
                    .Include(l => l.Images)
                    .Include(l => l.Seller)
                    .FirstOrDefaultAsync(l => l.ListingId == id && l.Status != "Removed");

                return listing is null
                    ? Results.NotFound()
                    : Results.Ok(new ReadBookDto(
                        listing.ListingId,
                        listing.Book.Title,
                        listing.Book.Author,
                        listing.Book.Category,
                        listing.Book.ISBN,
                        listing.Book.Description,
                        listing.Price,
                        listing.Condition,
                        listing.City,
                        listing.Status,
                        listing.Images.Select(img => img.ImageUrl).ToList(),
                        listing.Seller.Name,
                        listing.Seller.University
                    ));
            });


            group.MapPost("/", async ([FromForm] CreateBookDto newBook, ApplicationDbContext dbContext,
                                      IImageStorage storage, HttpContext http) =>
            {
                var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Results.Unauthorized();

                var seller = await dbContext.Users.FindAsync(userId);
                if (seller is null)
                    return Results.Unauthorized();

                var images = newBook.Images;

                //validate every file up front nothing is written to disk yet.
                var validated = new List<(IFormFile File, string Ext)>();
                if (images is not null)
                {
                    if (images.Count > storage.MaxFilesPerListing)
                        return Results.BadRequest(new { error = $"Maximum {storage.MaxFilesPerListing} images allowed per listing" });

                    foreach (var file in images)
                    {
                        var result = await storage.ValidateAsync(file);
                        if (!result.Ok)
                            return Results.BadRequest(new { error = result.Error });

                        validated.Add((file, result.Extension!));
                    }
                }

                Book book = new()
                {
                    Title = newBook.Title,
                    Author = string.IsNullOrWhiteSpace(newBook.Author) ? "Unknown" : newBook.Author,
                    Category = newBook.Category,
                    ISBN = string.IsNullOrWhiteSpace(newBook.Isbn) ? "Unknown" : newBook.Isbn,
                    Description = newBook.Description,
                    Listings = new List<Listing>()
                };

                Listing listing = new()
                {
                    Book = book,
                    Seller = seller,
                    Price = newBook.Price,
                    Condition = newBook.Condition,
                    City = newBook.City,
                    Status = "Available",
                    CreatedAt = DateTime.UtcNow,
                    Images = new List<ListingImage>()
                };

                //save files, then save to the DB.
                // If the DB save fails, delete whatever was written so we don't leave non used files.
                var savedUrls = new List<string>();
                try
                {
                    foreach (var (file, ext) in validated)
                    {
                        var url = await storage.SaveAsync(file, ext);
                        savedUrls.Add(url);

                        listing.Images.Add(new ListingImage
                        {
                            ImageUrl = url,
                            Listing = listing
                        });
                    }

                    dbContext.Listings.Add(listing);
                    await dbContext.SaveChangesAsync();
                }
                catch
                {
                    foreach (var url in savedUrls) storage.Delete(url);
                    throw;
                }

                return Results.Created($"/api/books/{listing.ListingId}", new { listingId = listing.ListingId });
            })
            .WithMetadata(new RequestSizeLimitAttribute(MaxRequestBytes))
            .AddEndpointFilter<ValidationFilter<CreateBookDto>>()
            .DisableAntiforgery()
            .RequireAuthorization();


            group.MapPut("/{id}", async (int id, [FromForm] UpdateBookDto updatedBook, ApplicationDbContext dbContext, HttpContext http) =>
            {
                var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Results.Unauthorized();


                var existingListing = await dbContext.Listings
                    .Include(l => l.Book)
                    .FirstOrDefaultAsync(l => l.ListingId == id && l.SellerId == userId);

                if (existingListing is null)
                    return Results.NotFound();


                existingListing.Book.Title = updatedBook.Title;
                existingListing.Book.Author = string.IsNullOrWhiteSpace(updatedBook.Author) ? "Unknown" : updatedBook.Author;
                existingListing.Book.Category = updatedBook.Category;
                existingListing.Book.ISBN = string.IsNullOrWhiteSpace(updatedBook.Isbn) ? "Unknown" : updatedBook.Isbn;
                existingListing.Book.Description = updatedBook.Description;

                existingListing.Price = updatedBook.Price;
                existingListing.Condition = updatedBook.Condition;
                existingListing.City = updatedBook.City;

                if (updatedBook.Status is not null)
                    existingListing.Status = updatedBook.Status;

                await dbContext.SaveChangesAsync();

                return Results.Ok(new { listingId = existingListing.ListingId });
            })
            .AddEndpointFilter<ValidationFilter<UpdateBookDto>>()
            .DisableAntiforgery()
            .RequireAuthorization();


            group.MapDelete("/{id}", async (int id, ApplicationDbContext dbContext, IImageStorage storage, HttpContext http) =>
            {
                var userIdClaim = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Results.Unauthorized();


                var listing = await dbContext.Listings
                    .Include(l => l.Images)
                    .FirstOrDefaultAsync(l => l.ListingId == id && l.SellerId == userId);

                if (listing is null)
                    return Results.NotFound();

                foreach (var image in listing.Images)
                {
                    storage.Delete(image.ImageUrl);
                }

                listing.Status = "Removed";
                await dbContext.SaveChangesAsync();

                return Results.NoContent();
            }).RequireAuthorization();
        }
    }
}