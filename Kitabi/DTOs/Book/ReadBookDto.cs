namespace Kitabi.Dtos.Book
{
    public record ReadBookDto(
        int ListingId,
        string Title,
        string? Author,
        string Category,
        string? Isbn,
        string Description,
        decimal Price,
        string Condition,
        string City,
        string Status,
        List<string> ImageUrls,
        string SellerName,
        string SellerUniversity
    );
}