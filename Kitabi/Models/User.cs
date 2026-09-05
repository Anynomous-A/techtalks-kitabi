using System.ComponentModel.DataAnnotations;

namespace Kitabi.Models;

public class User
{
    public int UserId { get; set; }
    public required string Name { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public required string Phone { get; set; }

    public required string University { get; set; }

    public required ICollection<Listing> Listings { get; set; }


}

