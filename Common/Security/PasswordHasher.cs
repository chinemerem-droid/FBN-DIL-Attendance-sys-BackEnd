using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace Employee_History.Common.Security
{
    /// <summary>
    /// PBKDF2-HMACSHA256 password hashing. Current format:
    /// <c>$PBKDF2$v=3$iter=100000${salt}${hash}</c>. Also verifies the legacy
    /// <c>$bcrypt$v=2$rounds=10$</c> format (PBKDF2 at 10,000 iterations) so
    /// passwords stored before v2 keep working.
    /// </summary>
    public static class PasswordHasher
    {
        private const int Pbkdf2Iterations = 100_000;
        private const int LegacyPbkdf2Iterations = 10_000;

        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(128 / 8);
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: Pbkdf2Iterations,
                numBytesRequested: 256 / 8));

            return $"$PBKDF2$v=3$iter={Pbkdf2Iterations}${Convert.ToBase64String(salt)}${hashed}";
        }

        public static bool VerifyPassword(string inputPassword, string? storedHashedPassword)
        {
            if (string.IsNullOrEmpty(inputPassword) || string.IsNullOrEmpty(storedHashedPassword))
            {
                return false;
            }

            var parts = storedHashedPassword.Split('$');
            if (parts.Length != 6)
            {
                return false;
            }

            int iterations;
            if (parts[1] == "PBKDF2" && parts[2] == "v=3" && parts[3].StartsWith("iter=")
                && int.TryParse(parts[3].Substring(5), out var iter))
            {
                iterations = iter;
            }
            else if (parts[1] == "bcrypt" && parts[2] == "v=2" && parts[3] == "rounds=10")
            {
                iterations = LegacyPbkdf2Iterations;
            }
            else
            {
                return false;
            }

            byte[] salt;
            try
            {
                salt = Convert.FromBase64String(parts[4]);
            }
            catch (FormatException)
            {
                return false;
            }

            string hashedInput = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: inputPassword,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: iterations,
                numBytesRequested: 256 / 8));

            return CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(hashedInput),
                Convert.FromBase64String(parts[5]));
        }

        /// <summary>Cryptographically random 12-character initial password.</summary>
        public static string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$%";
            var bytes = RandomNumberGenerator.GetBytes(12);
            var result = new char[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                result[i] = chars[bytes[i] % chars.Length];
            }
            return new string(result);
        }
    }
}
