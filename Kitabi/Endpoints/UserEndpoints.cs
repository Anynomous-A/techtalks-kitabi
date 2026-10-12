using System.Security.Claims;
using Kitabi.Data;
using Kitabi.DTOs;
using Kitabi.DTOs.User;
using Kitabi.Filters;
using Microsoft.EntityFrameworkCore;

namespace Kitabi.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        // Every route here is about the signed-in user only (id comes from the JWT, never from the URL).
        var group = app.MapGroup("/api/users/me").RequireAuthorization();

        // GET /api/users/me  -> the profile
        group.MapGet("/", async (ClaimsPrincipal principal, ApplicationDbContext db) =>
        {
            if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();

            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
            if (user is null) return Results.Unauthorized();

            return Results.Ok(new UserResponseDto(user.UserId, user.Email, user.Name, user.Phone, user.University));
        });

        // PUT /api/users/me  -> edit email / phone / university (name is the login, so it stays fixed)
        group.MapPut("/", async (UpdateUserDto model, ClaimsPrincipal principal, ApplicationDbContext db) =>
        {
            if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();

            var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user is null) return Results.Unauthorized();

            var email = model.Email!.Trim().ToLower();

            var emailTaken = await db.Users.AnyAsync(u => u.Email == email && u.UserId != userId);
            if (emailTaken)
                return Results.BadRequest(new { error = "Email already registered" });

            user.Email = email;
            user.Phone = model.Phone!.Trim();
            user.University = model.University!.Trim();
            await db.SaveChangesAsync();

            return Results.Ok(new UserResponseDto(user.UserId, user.Email, user.Name, user.Phone, user.University));
        })
        .AddEndpointFilter<ValidationFilter<UpdateUserDto>>();

        // GET /api/users/me/listings  -> my listings (Available + Sold; "Removed" ones stay hidden)
        group.MapGet("/listings", async (ClaimsPrincipal principal, ApplicationDbContext db) =>
        {
            if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return Results.Unauthorized();

            var listings = await db.Listings
                .AsNoTracking()
                .Where(l => l.SellerId == userId && l.Status != "Removed")
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new MyListingDto(
                    l.ListingId,
                    l.Book.Title,
                    l.Book.Author,
                    l.Book.Category,
                    l.Book.ISBN,
                    l.Book.Description,
                    l.Price,
                    l.Condition,
                    l.City,
                    l.Status,
                    l.CreatedAt,
                    l.Images.Select(i => new MyListingImageDto(i.ImageId, i.ImageUrl)).ToList()))
                .ToListAsync();

            return Results.Ok(listings);
        });
    }
}