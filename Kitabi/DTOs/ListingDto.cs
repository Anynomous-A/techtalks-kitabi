namespace Kitabi.DTOs
{
    public class ListingDto
    {
        public int ListingId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public string Condition { get; set; }
        public string City { get; set; }
        public string Status { get; set; }
        public string SellerName { get; set; }
    }

}
