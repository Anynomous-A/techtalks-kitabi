namespace Kitabi.Models
{
    public class Token
    {
        public int TokenId { get; set; }

        public int UserId { get; set; }

        public string RefreshToken { get; set; } = null!;

        public DateTime Expire { get; set; }

        public User User { get; set; } = null!;
    }
}