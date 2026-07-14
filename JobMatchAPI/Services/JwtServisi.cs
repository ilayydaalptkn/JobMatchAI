using JobMatchAPI.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace JobMatchAPI.Services
{
    public class JwtServisi
    {
        private readonly string _anahtar;

        public JwtServisi(IConfiguration configuration)
        {
            var key = configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(key))
                key = Environment.GetEnvironmentVariable("JWT_KEY");
            if (string.IsNullOrWhiteSpace(key))
                key = "JobMatchAI-Dev-Only-Key-Change-In-Production-2026!";

            _anahtar = key;
        }

        public string TokenUret(Kullanici kullanici)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, kullanici.Id.ToString()),
                new Claim(ClaimTypes.Email, kullanici.Eposta),
                new Claim(ClaimTypes.Role, kullanici.Rol)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_anahtar));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string AnahtarAl() => _anahtar;
    }
}
