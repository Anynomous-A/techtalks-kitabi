using Kitabi.Data;
using Kitabi.Dtos.Book;
using Kitabi.Filters;
using Kitabi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kitabi.Endpoints
{
    public static class BookEndpoints
    {
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

    
            group.MapPost("/", async ([FromForm] CreateBookDto newBook, ApplicationDbContext dbContext) =>
           {

                int sellerId = 1;

                var seller = await dbContext.Users.FindAsync(sellerId);
                if (seller is null)
                    return Results.BadRequest(new { error = "Test user not found. Please create a user with ID = 1" });

                var images = newBook.Images;
                
                if (images is not null)
             {
                 var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg", "image/gif" };
                 const long maxFileSize = 5 * 1024 * 1024; 
                 const int maxImages = 10;

       
                if (images.Count > maxImages)
                return Results.BadRequest(new { error = $"Maximum {maxImages} images allowed per listing" });

                foreach (var file in images)
             {
            
                if (!allowedTypes.Contains(file.ContentType))
                return Results.BadRequest(new { error = $"Invalid file type: {file.FileName}. Only JPEG, PNG, and GIF allowed" });

                
          
               if (file.Length > maxFileSize)
                return Results.BadRequest(new { error = $"File too large: {file.FileName}. Maximum 5MB" });
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
                    SellerId = newBook.SellerId,
                    Price = newBook.Price,
                    Condition = newBook.Condition,
                    City = newBook.City,
                    Status = "Available",
                    CreatedAt = DateTime.UtcNow,
                    Images = new List<ListingImage>()
                };

                if (images is not null)
                {   
                    var uploadDir = Path.Combine("wwwroot", "uploads");
                    Directory.CreateDirectory(uploadDir);

                    foreach (var file in images)
                    {
                        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                        var path = Path.Combine(uploadDir, fileName);
                        using var stream = File.Create(path);
                        await file.CopyToAsync(stream);

                        listing.Images.Add(new ListingImage
                        {
                            ImageUrl = $"/uploads/{fileName}",
                            Listing = listing
                        });
                    }
                }

                dbContext.Listings.Add(listing);
                await dbContext.SaveChangesAsync();

                return Results.Created($"/api/books/{listing.ListingId}", new { listingId = listing.ListingId });
            })
            .AddEndpointFilter<ValidationFilter<CreateBookDto>>()
            .DisableAntiforgery();

       
            group.MapPut("/{id}", async (int id, [FromForm] UpdateBookDto updatedBook, ApplicationDbContext dbContext) =>
            {
                var existingListing = await dbContext.Listings
                    .Include(l => l.Book)
                    .FirstOrDefaultAsync(l => l.ListingId == id);

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
            .AddEndpointFilter<ValidationFilter<UpdateBookDto>>();

           
          group.MapDelete("/{id}", async (int id, ApplicationDbContext dbContext) =>
        {
             var listing = await dbContext.Listings
            .Include(l => l.Images) 
            .FirstOrDefaultAsync(l => l.ListingId == id);

            if (listing is null)
            return Results.NotFound();

   
            foreach (var image in listing.Images)
            {
            var filePath = Path.Combine("wwwroot", image.ImageUrl.TrimStart('/'));
            if (File.Exists(filePath))
            File.Delete(filePath);
            }

            listing.Status = "Removed";
            await dbContext.SaveChangesAsync();

             return Results.NoContent();
         });
        }
    }
}