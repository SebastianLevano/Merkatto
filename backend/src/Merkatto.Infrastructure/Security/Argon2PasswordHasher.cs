using Isopoh.Cryptography.Argon2;
using Merkatto.Application.Common;

namespace Merkatto.Infrastructure.Security;

/// <summary>Password hashing with Argon2id (memory-hard, salted, encoded string output).</summary>
public sealed class Argon2PasswordHasher : IPasswordHasher
{
    public string Hash(string password) => Argon2.Hash(password);

    public bool Verify(string password, string hash) => Argon2.Verify(hash, password);
}
