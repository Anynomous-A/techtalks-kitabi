using Kitabi.Models;
using Microsoft.EntityFrameworkCore;


namespace Kitabi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Listing> Listings { get; set; }
        public DbSet<ListingImage> ListingImages { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Token> Tokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ListingImage>().HasKey(i => i.ImageId);

            //Name
            modelBuilder.Entity<User>()
            .HasIndex(u => u.Name)
            .IsUnique();

            // User
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();


            // Listing → Book
            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Book)
                .WithMany(b => b.Listings)
                .HasForeignKey(l => l.BookId);


            // Listing → Seller (User)
            modelBuilder.Entity<Listing>()
                .HasOne(l => l.Seller)
                .WithMany(u => u.Listings)
                .HasForeignKey(l => l.SellerId)
                .OnDelete(DeleteBehavior.Restrict);


            // Listing → Images
            modelBuilder.Entity<ListingImage>()
                .HasOne(i => i.Listing)
                .WithMany(l => l.Images)
                .HasForeignKey(i => i.ListingId);


            // Contact → Listing
            modelBuilder.Entity<Contact>()
                .HasOne(c => c.Listing)
                .WithMany()
                .HasForeignKey(c => c.ListingId);


            // Contact → Buyer (User)
            modelBuilder.Entity<Contact>()
                .HasOne(c => c.Buyer)
                .WithMany()
                .HasForeignKey(c => c.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);


            // Token → User
            modelBuilder.Entity<Token>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            // Price
            modelBuilder.Entity<Listing>()
                .Property(x => x.Price)
                .HasPrecision(18, 2);
        }
    }
}