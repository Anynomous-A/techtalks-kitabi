namespace Kitabi.Models
{
    public class Book
    {
        public int BookId { get; set; }

        public required string Title { get; set; }

        public required string Author { get; set; }

        public required string Category { get; set; }

        public required string ISBN { get; set; }

        public required string Description { get; set; }

        public required ICollection<Listing> Listings { get; set; }
    }
}