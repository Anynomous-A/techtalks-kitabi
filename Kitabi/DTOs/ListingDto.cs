namespace Kitabi.DTOs
{
    public class ListingDto
    {
        public int ListingId { get; set; }
        public required string Title { get; set; }
        public required string Author { get; set; }
        public required string Category { get; set; }
        public decimal Price { get; set; }
        public required string Condition { get; set; }
        public required string City { get; set; }
        public required string Status { get; set; }
        public required string SellerName { get; set; }
    }

}
