namespace Kitabi.Interfaces
{
    public interface IPasswordHasher
    {
        string Hash(string Password);

        bool Verify(string hashedPassword, string providedPassword);

    }
}