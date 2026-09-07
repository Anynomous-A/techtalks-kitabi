using System.ComponentModel.DataAnnotations;

namespace Kitabi.Dtos.Book
{
    public record CreateBookDto(
        
        [property: Required(ErrorMessage = "Title is required")]
        [property: StringLength(100, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 100 characters")]
        string Title,


        [property: StringLength(100, ErrorMessage = "Author must not exceed 100 characters")]
        string? Author,

        [property: Required(ErrorMessage = "Category is required")]
        [property: StringLength(60, ErrorMessage = "Category must not exceed 60 characters")]
        string Category,

        [property: StringLength(20, ErrorMessage = "ISBN must not exceed 20 characters")]
        string? Isbn,
        
        [property: Required(ErrorMessage = "Description is required")]
        [property: StringLength(1000, ErrorMessage = "Description must not exceed 1000 characters")]
        string Description,

        [property: Required(ErrorMessage = "Price is required")]
        [property: Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        decimal Price, 

        [property: Required(ErrorMessage = "Condition is required")]
        [property: StringLength(60, ErrorMessage = "Condition must not exceed 60 characters")]
        string Condition, 

        [property: Required(ErrorMessage = "City is required")]
        [property: StringLength(80, ErrorMessage = "City name must not exceed 80 characters")]
        string City,

        [property: Required(ErrorMessage = "Seller ID is required")]
        int SellerId,

        IFormFileCollection? Images
    );
}