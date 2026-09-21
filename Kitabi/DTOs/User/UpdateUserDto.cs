using System.ComponentModel.DataAnnotations;

namespace Kitabi.DTOs.User;

public record UpdateUserDto(

    [property: Required(ErrorMessage = "Email is required")]
    [property: EmailAddress(ErrorMessage = "Invalid email format")]
    [property: StringLength(100, ErrorMessage = "Email must not exceed 100 characters")]
    string? Email,

    [property: Required(ErrorMessage = "Phone number is required")]
    [property: RegularExpression(@"^(70|71|76|78|79|81|03|30|31|36|38|39)\d{6}$",
        ErrorMessage = "Phone number must be a valid Lebanese number (e.g., 70123456, 81234567)")]
    string? Phone,

    [property: Required(ErrorMessage = "University is required")]
    [property: StringLength(100, ErrorMessage = "University name must not exceed 100 characters")]
    string? University
);
