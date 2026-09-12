using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebDeveloper.Models.Entities;

namespace WebDeveloper.Security
{
    /// <summary>
    /// Tạo và validate JWT tokens (tương đương JwtTokenProvider.java)
    /// </summary>
    public class JwtTokenProvider
    {
        private readonly IConfiguration _config;

        public JwtTokenProvider(IConfiguration config)
        {
            _config = config;
        }

        private SymmetricSecurityKey GetSigningKey()
        {
            var secretKey = _config["Jwt:SecretKey"]!;
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        }

        public string CreateToken(User user)
        {
            var expirationMinutes = int.Parse(_config["Jwt:ExpirationMinutes"] ?? "1440");
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role?.ToString() ?? "")
            };

            var credentials = new SigningCredentials(GetSigningKey(), SecurityAlgorithms.HmacSha512);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                SigningCredentials = credentials,
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public int? GetUserIdFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParams = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = GetSigningKey(),
                    ValidateIssuer = true,
                    ValidIssuer = _config["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _config["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParams, out _);
                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
                return userIdClaim != null ? int.Parse(userIdClaim.Value) : null;
            }
            catch
            {
                return null;
            }
        }

        public DateTime? GetExpiryDateFromToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                return jwtToken.ValidTo;
            }
            catch
            {
                return null;
            }
        }

        public string? ResolveToken(HttpRequest request)
        {
            var bearerToken = request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(bearerToken) && bearerToken.StartsWith("Bearer "))
            {
                return bearerToken["Bearer ".Length..];
            }
            return null;
        }
    }
}
