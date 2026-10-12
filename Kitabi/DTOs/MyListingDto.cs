namespace Kitabi.DTOs
{
    public record MyListingImageDto(int ImageId, string ImageUrl);

    // One of the signed-in user's own listings, with everything the profile page needs to show/edit it.
    public record MyListingDto(
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
        DateTime CreatedAt,
        List<MyListingImageDto> Images
    );
}