using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using JwtAuthApp.Models;
using Microsoft.Extensions.Logging;


namespace JwtAuthApp.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly TimeSpan _tokenLifetime;
        private readonly int _iterations;
        private const int LegacyIterations = 10000; // итерации, использовавшиеся до повышения (для обратной совместимости)

        public AuthService(IConfiguration configuration, ILogger<AuthService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var expireDays = configuration.GetValue<double?>("Jwt:ExpireDays") ?? 1;
            _tokenLifetime = TimeSpan.FromDays(expireDays);

            // Рекомендация OWASP для PBKDF2-HMAC-SHA256 — не менее 600k; 210k — компромисс для совместимости
            _iterations = configuration.GetValue<int?>("Security:PasswordIterations") ?? 210000;
            if (_iterations < LegacyIterations)
                _iterations = LegacyIterations;
        }

        public string GenerateJwtToken(User user)
        {
            try
            {
                var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
                };

                // Добавляем claim для каждой роли пользователя
                foreach (var userRole in user.UserRoles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
                }

                // Обратная совместимость: если UserRoles пусто, используем поле Role
                if (!user.UserRoles.Any())
                {
                    claims.Add(new Claim(ClaimTypes.Role, user.Role));
                }

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                    _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured")));
                
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.Add(_tokenLifetime),
                    signingCredentials: creds);

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for user {UserName}", user.UserName); // Изменено
                throw;
            }
        }

        public (string hash, string salt) HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            // Используем более безопасный алгоритм с большим количеством итераций
            using var rng = RandomNumberGenerator.Create();
            byte[] saltBytes = new byte[32]; // Увеличиваем размер соли
            rng.GetBytes(saltBytes);
            string salt = Convert.ToBase64String(saltBytes);

            // Используем PBKDF2 для более безопасного хеширования
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password, 
                saltBytes, 
                _iterations, // Количество итераций (настраивается в Security:PasswordIterations)
                HashAlgorithmName.SHA256);
            
            byte[] hashBytes = pbkdf2.GetBytes(32);
            string hash = Convert.ToBase64String(hashBytes);

            return (hash, salt);
        }

        public bool VerifyPassword(string password, string hash, string salt)
        {
            try
            {
                byte[] saltBytes = Convert.FromBase64String(salt);
                byte[] storedHash = Convert.FromBase64String(hash);

                using var pbkdf2 = new Rfc2898DeriveBytes(
                    password,
                    saltBytes,
                    _iterations,
                    HashAlgorithmName.SHA256);
                
                byte[] computedHash = pbkdf2.GetBytes(32);
                
                // Сравнение с защитой от атак по времени
                if (CryptographicOperations.FixedTimeEquals(computedHash, storedHash))
                    return true;

                // Обратная совместимость: хеши, созданные со старым числом итераций (10000).
                // При следующей смене пароля пользователь будет пере-хеширован с актуальными итерациями.
                if (_iterations != LegacyIterations)
                {
                    using var legacyPbkdf2 = new Rfc2898DeriveBytes(
                        password,
                        saltBytes,
                        LegacyIterations,
                        HashAlgorithmName.SHA256);
                    byte[] legacyHash = legacyPbkdf2.GetBytes(32);
                    return CryptographicOperations.FixedTimeEquals(legacyHash, storedHash);
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                return tokenHandler.ValidateToken(token, validationParameters, out _);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return null;
            }
        }
    }
}