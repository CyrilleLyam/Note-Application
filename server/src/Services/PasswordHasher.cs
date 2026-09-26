using Isopoh.Cryptography.Argon2;
using server.src.Services.Interfaces;

namespace server.src.Services;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return Argon2.Hash(password);
    }

    public bool Verify(string hashed, string password)
    {
        return Argon2.Verify(hashed, password);
    }
}
