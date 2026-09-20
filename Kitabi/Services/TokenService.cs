using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Kitabi.Data;
using Kitabi.DTOs.Auth;
using Kitabi.DTOs.User;
using Kitabi.Interfaces;
using Kitabi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Kitabi.Services;

public class TokenService(ApplicationDbContext dbContext , IConfiguration configuration) : ITokenService
{
   
  
    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim("Verified", "true")
        };

        var token = new JwtSecurityToken(
            issuer: "MyClient",
            audience: "MyServer",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> GenerateRefreshTokenAsync(User user)
    {
        var newRefreshToken = Guid.NewGuid().ToString();
        var token = new Token { UserId = user.UserId, RefreshToken = newRefreshToken, Expire = DateTime.UtcNow.AddDays(7) };



        dbContext.Tokens.Add(token);
        await dbContext.SaveChangesAsync();

        return newRefreshToken;
    }

    public async Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken)
    {
        var token = await dbContext.Tokens
             .Include(t => t.User)
             .FirstOrDefaultAsync(t => t.RefreshToken == refreshToken);

        if (token == null || token.Expire < DateTime.UtcNow)
            return null;

        dbContext.Tokens.Remove(token);

        var newAccessToken = GenerateAccessToken(token.User);
        var newRefreshToken = await GenerateRefreshTokenAsync(token.User);

        var userResponse = new UserResponseDto(
            token.User.UserId,
            token.User.Email,
            token.User.Name,
            token.User.Phone,
            token.User.University

        );

        return new AuthResponseDto(newAccessToken, newRefreshToken, userResponse);
    }
}