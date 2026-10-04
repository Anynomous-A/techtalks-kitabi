using System.ComponentModel.DataAnnotations;

namespace Kitabi.DTOs.User;

public record RegisterDto(

    [property: Required(ErrorMessage = "Name is required")]
    [property: StringLength(50, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 50 characters")]
    [property: RegularExpression(@"^[a-zA-Z0-9_ ]+$",
        ErrorMessage = "Name can only contain letters, numbers, underscores, and spaces.")]
    string Name,

    [property: Required(ErrorMessage = "Email is required")]
    [property: EmailAddress(ErrorMessage = "Invalid email format")]
    string Email,

    [property: Required(ErrorMessage = "Password is required")]
    [property: StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    [property: RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).+$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number.")]
    string Password,

    [property: Required(ErrorMessage = "Phone number is required")]
    [property: RegularExpression(@"^(70|71|76|78|79|81|03|30|31|36|38|39)\d{6}$",
        ErrorMessage = "Phone number must be a valid Lebanese number (e.g., 70123456, 81234567)")]
    string Phone,

    [property: Required(ErrorMessage = "University is required")]
    [property: StringLength(100, ErrorMessage = "University name must not exceed 100 characters")]
    string University
);
