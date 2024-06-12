using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Employee_History.Models;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

public static class JwtTokenGenerator
{
    public static string GenerateToken(User user, IConfiguration configuration)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var secretKey = configuration["Jwt:SecretKey"];

        if (string.IsNullOrEmpty(secretKey))
        {
            throw new ArgumentException("Missing 'Jwt:SecretKey' in appsettings.json");
        }

        var keyByteArray = Encoding.ASCII.GetBytes(secretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Staff_ID),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim("LabRole", user.Lab_role)
            }),
            Expires = DateTime.UtcNow.AddMinutes(20),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyByteArray), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
