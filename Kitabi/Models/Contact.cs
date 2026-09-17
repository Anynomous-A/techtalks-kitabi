namespace Kitabi.Models
{
    public class Contact
    {
        public int ContactId { get; set; }

        public int ListingId { get; set; }

        public int BuyerId { get; set; }

        public DateTime CreatedAt { get; set; }

        public required string Method { get; set; }

        public required Listing Listing { get; set; }

        public required User Buyer { get; set; }
    }
}