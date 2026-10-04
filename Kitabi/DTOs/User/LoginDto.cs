using System.ComponentModel.DataAnnotations;

namespace Kitabi.DTOs.User;

public record LoginDto(
    [property: Required(ErrorMessage = "Name is required")]
    [property: StringLength(50, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 50 characters")]
    [property: RegularExpression(@"^[a-zA-Z0-9_ ]+$",
        ErrorMessage = "Name can only contain letters, numbers, underscores, and spaces.")]
    string Name,

    [property: Required(ErrorMessage = "Password is required")]
    [property: StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    [property: RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).+$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number.")]
    string Password
);
