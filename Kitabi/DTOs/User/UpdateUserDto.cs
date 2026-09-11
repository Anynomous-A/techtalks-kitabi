using System.ComponentModel.DataAnnotations;

namespace Kitabi.DTOs.User;

public record UpdateUserDto(



    [property: Required(ErrorMessage = "Email is required")]
    [property: EmailAddress(ErrorMessage = "Invalid email format")]
    string? Email,

    [property: Required(ErrorMessage = "PhoneNumber is required")]
    [property: RegularExpression(@"^(70|71|76|78|79|81|03|30|31|36|38|39)\d{6}$",
        ErrorMessage = "Phone number must be a valid Lebanese number (e.g., 70123456, 81234567)")]
    string? Phone,

    [property: Required(ErrorMessage = "University is required")]
    string? University
);