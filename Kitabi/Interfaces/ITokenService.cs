using Kitabi.DTOs.Auth;
using Kitabi.Models;


namespace Kitabi.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    Task<string> GenerateRefreshTokenAsync(User user);
    Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken);
}