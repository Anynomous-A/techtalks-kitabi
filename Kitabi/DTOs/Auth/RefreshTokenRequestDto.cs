using System.ComponentModel.DataAnnotations;

namespace Kitabi.DTOs.Auth;

public record RefreshTokenRequestDto(
    [property: Required(ErrorMessage = "Refresh token is required")]
    string RefreshToken
);