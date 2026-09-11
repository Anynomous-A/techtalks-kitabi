namespace Kitabi.DTOs.User;

public record UserResponseDto(
    int UserId,
    string Email,
    string Name,
    string Phone,
    string University

);