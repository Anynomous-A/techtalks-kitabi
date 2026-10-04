using System.ComponentModel.DataAnnotations;

namespace Kitabi.DTOs.Auth;

public record RefreshTokenRequestDto(
    [property: StringLength(200, ErrorMessage = "Refresh token cannot exceed 200 characters")]
    [property: RegularExpression(@"^[A-Za-z0-9\-_\.]*$", ErrorMessage = "Refresh token contains invalid characters")]
    string? RefreshToken
);
