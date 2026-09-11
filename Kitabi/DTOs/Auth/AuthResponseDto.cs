using Kitabi.DTOs.User;

namespace Kitabi.DTOs.Auth;

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    UserResponseDto User
);