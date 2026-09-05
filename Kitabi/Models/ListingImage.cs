namespace Kitabi.Models
{
    public class ListingImage
    {
        public int ImageId { get; set; }

        public int ListingId { get; set; }

        public required string ImageUrl { get; set; }

        public required Listing Listing { get; set; }
    }
}