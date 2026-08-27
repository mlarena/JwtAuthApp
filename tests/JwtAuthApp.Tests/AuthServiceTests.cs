using JwtAuthApp.Models;
using JwtAuthApp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;

namespace JwtAuthApp.Tests
{
    public class AuthServiceTests
    {
        private static IConfiguration BuildConfig(int? iterations = null)
        {
            var values = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TestSecretKey_At_Least_32_Characters_Long_1234",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:ExpireDays"] = "1",
                ["Security:PasswordIterations"] = (iterations ?? 210000).ToString()
            };
            return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        }

        private static AuthService CreateService(int? iterations = null)
            => new(BuildConfig(iterations), NullLogger<AuthService>.Instance);

        private static User CreateUser(params string[] roleNames)
        {
            var user = new User
            {
                UserName = "tester",
                PasswordHash = "x",
                Salt = "y",
                Role = roleNames.FirstOrDefault() ?? "User"
            };
            user.UserRoles = roleNames.Select(r => new UserRole { Role = new Role { Name = r } }).ToList();
            return user;
        }

        [Fact]
        public void HashPassword_And_VerifyPassword_Roundtrip()
        {
            var service = CreateService();
            var (hash, salt) = service.HashPassword("MyStrongPass123");

            Assert.False(string.IsNullOrEmpty(hash));
            Assert.False(string.IsNullOrEmpty(salt));
            Assert.True(service.VerifyPassword("MyStrongPass123", hash, salt));
        }

        [Fact]
        public void VerifyPassword_ReturnsFalse_ForWrongPassword()
        {
            var service = CreateService();
            var (hash, salt) = service.HashPassword("CorrectPass123");

            Assert.False(service.VerifyPassword("WrongPass", hash, salt));
        }

        [Fact]
        public void HashPassword_Produces_DifferentHashes_ForSamePassword()
        {
            var service = CreateService();
            var (hash1, _) = service.HashPassword("SamePassword");
            var (hash2, _) = service.HashPassword("SamePassword");

            Assert.NotEqual(hash1, hash2); // разная соль
        }

        [Fact]
        public void VerifyPassword_Accepts_LegacyIterations_Hashes()
        {
            // Хеш, созданный со старым количеством итераций (10000), должен приниматься новым сервисом
            var legacyService = CreateService(iterations: 10000);
            var (hash, salt) = legacyService.HashPassword("LegacyPass");

            var currentService = CreateService(iterations: 210000);
            Assert.True(currentService.VerifyPassword("LegacyPass", hash, salt));
        }

        [Fact]
        public void VerifyPassword_Rejects_Passwords_WithWrongIterationCount_NewHash()
        {
            // Хеш, созданный с 210000 итераций, не должен верифицироваться сервисом с 10000 (новый не совпадает)
            var newService = CreateService(iterations: 210000);
            var (hash, salt) = newService.HashPassword("StrongPass");

            var legacyService = CreateService(iterations: 10000);
            Assert.False(legacyService.VerifyPassword("StrongPass", hash, salt));
        }

        [Fact]
        public void GenerateJwtToken_Produces_TokenWithRoles()
        {
            var service = CreateService();
            var user = CreateUser("Admin", "User");
            var token = service.GenerateJwtToken(user);

            var principal = service.ValidateToken(token);
            Assert.NotNull(principal);

            var roles = principal!.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            Assert.Contains("Admin", roles);
            Assert.Contains("User", roles);
            Assert.Equal("tester", principal.Identity?.Name);
        }

        [Fact]
        public void ValidateToken_ReturnsNull_ForGarbageToken()
        {
            var service = CreateService();
            Assert.Null(service.ValidateToken("not-a-jwt"));
        }

        [Fact]
        public void ValidateToken_Rejects_TokenFromAnotherIssuer()
        {
            var service = CreateService();
            var token = service.GenerateJwtToken(CreateUser("User"));

            var otherConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TestSecretKey_At_Least_32_Characters_Long_1234",
                ["Jwt:Issuer"] = "AnotherIssuer",
                ["Jwt:Audience"] = "TestAudience"
            }).Build();
            var otherService = new AuthService(otherConfig, NullLogger<AuthService>.Instance);

            Assert.Null(otherService.ValidateToken(token));
        }
    }
}