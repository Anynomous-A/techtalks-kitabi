using Kitabi.Data;
using Kitabi.DTOs.Auth;
using Kitabi.DTOs.User;
using Kitabi.Filters;
using Kitabi.Interfaces;
using Kitabi.Models;
using Microsoft.EntityFrameworkCore;

namespace CafeBackend.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");


        group.MapPost("/register", async (RegisterDto model, ApplicationDbContext db, IPasswordHasher passwordHasher) =>
        {
            var email = model.Email.Trim().ToLower();
            var name = model.Name.Trim();


            var emailExists = await db.Users.AnyAsync(u => u.Email == email);
            if (emailExists)
                return Results.BadRequest(new { error = "Email already registered" });


            var nameExists = await db.Users.AnyAsync(u => u.Name == name);
            if (nameExists)
                return Results.BadRequest(new { error = "Name already taken" });


            var user = new User
            {
                Name = name,
                Email = email,
                PasswordHash = passwordHasher.Hash(model.Password),
                Phone = model.Phone.Trim(),
                University = model.University.Trim(),
                Listings = new List<Listing>()


            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "User registered successfully" });
        })
        .AddEndpointFilter<ValidationFilter<RegisterDto>>();

        group.MapPost("/login", async (LoginDto model, ApplicationDbContext db, IPasswordHasher passwordHasher, ITokenService tokenService) =>
        {
            var name = model.Name.Trim();

            var user = await db.Users.FirstOrDefaultAsync(u => u.Name == name);
            if (user == null)
                return Results.BadRequest(new { error = "Invalid name or password" });

            var isValid = passwordHasher.Verify(user.PasswordHash, model.Password);
            if (!isValid)
                return Results.BadRequest(new { error = "Invalid name or password" });

            var accessToken = tokenService.GenerateAccessToken(user);
            var refreshToken = await tokenService.GenerateRefreshTokenAsync(user);

            var response = new AuthResponseDto(
                accessToken,
                refreshToken,
                new UserResponseDto(user.UserId, user.Email, user.Name, user.Phone, user.University)
            );

            return Results.Ok(new { message = "Login successful", data = response });
        })
        .AddEndpointFilter<ValidationFilter<LoginDto>>();


        group.MapPost("/refresh", async (RefreshTokenRequestDto model, ITokenService tokenService) =>
        {
            var result = await tokenService.RefreshTokenAsync(model.RefreshToken);

            if (result == null)
                return Results.Unauthorized();

            return Results.Ok(new { message = "Token refreshed successfully", data = result });
        })
        .AddEndpointFilter<ValidationFilter<RefreshTokenRequestDto>>();


        group.MapPost("/logout", async (RefreshTokenRequestDto model, ApplicationDbContext db) =>
        {
            var token = await db.Tokens.FirstOrDefaultAsync(t => t.RefreshToken == model.RefreshToken);
            if (token == null)
                return Results.BadRequest(new { error = "Failed to logout" });

            db.Tokens.Remove(token);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Logged out successfully" });
        })
        .AddEndpointFilter<ValidationFilter<RefreshTokenRequestDto>>();
    }
}