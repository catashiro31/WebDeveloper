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
    public class AccessTokenProvider
    {
        private readonly IConfiguration _config;

        public AccessTokenProvider(IConfiguration config)
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
            var expirationMinutes = int.Parse(_config["Jwt:ExpirationMinutes"] ?? "15"); // Short lived Access Token (15m)
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role?.ToString() ?? ""),
                new(ClaimTypes.Name, user.FullName ?? "Người dùng")
            };
            
            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                claims.Add(new Claim("AvatarUrl", user.AvatarUrl));
            }

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

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = GetSigningKey(),
                ValidateLifetime = false // Here we are saying that we don't care about the token's expiration date
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha512, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
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
