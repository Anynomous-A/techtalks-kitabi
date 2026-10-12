using Kitabi.Data;
using Kitabi.Models;
using Microsoft.EntityFrameworkCore;
using Kitabi.DTOs;
namespace Kitabi.Endpoints
{

    public static class ListingEndpoints
    {
        public static void MapListingEndpoints(this WebApplication app)
        {
            app.MapGet("/api/listings/search", async (
                ApplicationDbContext db,
                string? title,
                string? author,
                string? category,
                string? city,
                decimal? minPrice,
                decimal? maxPrice,
                int page = 1,
                int pageSize = 10,
                string sortBy = "createdAt",
                string sortOrder = "desc") =>
            {
                var query = db.Listings
                    .Include(l => l.Book)
                    .Include(l => l.Seller)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(title))
                    query = query.Where(l => l.Book.Title.Contains(title));

                if (!string.IsNullOrEmpty(author))
                    query = query.Where(l => l.Book.Author.Contains(author));

                if (!string.IsNullOrEmpty(category))
                    query = query.Where(l => l.Book.Category == category);

                if (!string.IsNullOrEmpty(city))
                    query = query.Where(l => l.City == city);

                if (minPrice.HasValue)
                    query = query.Where(l => l.Price >= minPrice);

                if (maxPrice.HasValue)
                    query = query.Where(l => l.Price <= maxPrice);

                // Sorting logic
                query = (sortBy.ToLower(), sortOrder.ToLower()) switch
                {
                    ("price", "asc") => query.OrderBy(l => l.Price),
                    ("price", "desc") => query.OrderByDescending(l => l.Price),
                    ("title", "asc") => query.OrderBy(l => l.Book.Title),
                    ("title", "desc") => query.OrderByDescending(l => l.Book.Title),
                    ("createdat", "asc") => query.OrderBy(l => l.CreatedAt),
                    ("createdat", "desc") => query.OrderByDescending(l => l.CreatedAt),
                    _ => query.OrderByDescending(l => l.CreatedAt) // default
                };

                var results = await query
                    .Select(l => new ListingDto
                    {
                        ListingId = l.ListingId,
                        Title = l.Book.Title,
                        Author = l.Book.Author,
                        Category = l.Book.Category,
                        Price = l.Price,
                        Condition = l.Condition,
                        City = l.City,
                        Status = l.Status,
                        SellerName = l.Seller.Name
                    })
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Results.Ok(results);
            });
        }
    }

}