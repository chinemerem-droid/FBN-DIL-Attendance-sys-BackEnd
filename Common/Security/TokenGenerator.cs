using Employee_History.Features.Users;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Employee_History.Common.Security
{
    /// <summary>
    /// Issues JWT access tokens (claims: nameid = staff id, unique_name = name,
    /// LabRole = A1/B2/C3) and URL-safe secure random tokens (refresh /
    /// password-reset tokens).
    /// </summary>
    public static class TokenGenerator
    {
        public static string GenerateAccessToken(User user, IConfiguration configuration)
        {
            var secretKey = configuration["Jwt:SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("Missing JWT secret key. Set the Jwt__SecretKey environment variable.");
            }

            var minutes = configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 60;
            var keyBytes = Encoding.ASCII.GetBytes(secretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Staff_ID),   // -> "nameid"
                    new Claim(ClaimTypes.Name, user.Name ?? string.Empty), // -> "unique_name"
                    new Claim("LabRole", user.Lab_role ?? string.Empty)
                }),
                Expires = DateTime.UtcNow.AddMinutes(minutes),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
        }

        /// <summary>32 random bytes, base64url-encoded (43 chars, URL-safe).</summary>
        public static string GenerateSecureToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
