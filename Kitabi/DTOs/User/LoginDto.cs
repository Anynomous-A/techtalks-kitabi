using System.ComponentModel.DataAnnotations;
namespace Kitabi.DTOs.User;

public record LoginDto(
    [property: Required(ErrorMessage = "Name is required")]
    [property: RegularExpression(@"^[a-zA-Z0-9_ ]+$",
    ErrorMessage = "FullName can only contain letters, numbers, underscores, and spaces.")]
    string Name,

    [property: Required(ErrorMessage = "Password is required")]
    string Password
);