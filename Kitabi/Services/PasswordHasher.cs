using Kitabi.Models;
using Kitabi.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Kitabi.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        public string Hash(string Password)
        {
            var hasher = new PasswordHasher<User>();
            return hasher.HashPassword(null!, Password);
        }

        public bool Verify(string hashedPassword, string providedPassword)
        {
            var hasher = new PasswordHasher<User>();

            var result = hasher.VerifyHashedPassword(null!, hashedPassword, providedPassword);

            return result == PasswordVerificationResult.Success;
        }
    }
}