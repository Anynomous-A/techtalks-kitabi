namespace Kitabi.Models
{
    public class Listing
    {
        public int ListingId { get; set; }

        public int BookId { get; set; }

        public int SellerId { get; set; }

        public decimal Price { get; set; }

        public required string Condition { get; set; }

        public required string City { get; set; }

        public required string Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public required Book Book { get; set; }

        public required User Seller { get; set; }

        public required ICollection<ListingImage> Images { get; set; }
    }
}